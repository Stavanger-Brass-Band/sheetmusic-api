using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using SheetMusic.Api.Users.Authorization;
using SheetMusic.Api.Users.Errors;
using SheetMusic.Api.Users.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace SheetMusic.Api.Test.Users;

[CollectionDefinition(Collections.User)]
public class UserTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    // --- V2 (Identity auth) ---

    private HttpClient CreateV2Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");
        return client;
    }

    private HttpClient CreateV2ClientWithTestToken(TestUser user)
    {
        var client = factory.CreateClientWithTestToken(user);
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");
        return client;
    }

    [Fact]
    public async Task V2_GetToken_WithMatchingUserPassword_ShouldBeSuccessful()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetToken_ShouldAssumeBasicGrantType_WhenGrantTypeIsOmitted()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetToken_ShouldAssumeBasicGrantType_WhenGrantTypeIsPassword()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "password"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiPasswordFlow_ShouldIssueTokens_WhenUsingAdvertisedTokenUrl()
    {
        var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("openapi/2.0.json"));
        var tokenUrl = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("oauth2")
            .GetProperty("flows")
            .GetProperty("password")
            .GetProperty("tokenUrl")
            .GetString();

        var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(
        [
            new("grant_type", "password"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        ]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<ApiAccessTokens>();
        tokens!.access_token.Should().NotBeNullOrWhiteSpace();
        tokens.token_type.Should().Be("bearer");
    }

    [Fact]
    public async Task V2_GetToken_WithWrongPassword_ShouldReturnBadRequest()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", "wrong-password")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Use a brand-new factory (own in-memory database and rate limiter state) with a low
        // limit, so this test doesn't affect the shared factory/database used by other tests.
        using var limitedFactory = new SheetMusicWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Token:PermitLimit", "2");
            builder.UseSetting("RateLimiting:Token:WindowSeconds", "60");
        });

        var client = limitedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        await client.PostAsync("token", new FormUrlEncodedContent(collection));
        await client.PostAsync("token", new FormUrlEncodedContent(collection));
        var response = await client.PostAsync("token", new FormUrlEncodedContent(collection));

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task V2_GetToken_WithNonExistentUser_ShouldReturnBadRequest()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", "nonexistent@user.com"),
            new("password", "anyPassword")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldReturnBadRequest_WhenUserIsInactive()
    {
        var client = CreateV2Client();
        var email = $"inactive-{Guid.NewGuid():N}@user.com";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Inactive User",
            Email = email,
            Password = "SecurePassword123!"
        });

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", email),
            new("password", "SecurePassword123!")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldCreateUser_WhenAnonymous()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "New User",
            Email = $"new-{Guid.NewGuid():N}@user.com",
            Password = "SecurePassword123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldReturnBadRequest_WhenDuplicateEmail()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Duplicate User",
            Email = TestUser.Testesen.Email,
            Password = "SecurePassword123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldReturnRequirementDetails_WhenWeakPassword()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Weak Password User",
            Email = $"weak-{Guid.NewGuid():N}@user.com",
            Password = "short"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<PasswordProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(nameof(PasswordRequirementsNotMetError));
        problem.FailedRequirements.Should().Contain("PasswordTooShort");
        problem.Messages.Should().NotBeNullOrEmpty();
        problem.Requirements.Should().NotBeNull();
        problem.Requirements!.MinimumLength.Should().Be(8);
    }

    [Fact]
    public async Task V2_GetPasswordRequirements_ShouldReturnConfiguredValues()
    {
        var client = CreateV2Client();

        var response = await client.GetAsync("users/password-requirements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var requirements = await response.Content.ReadFromJsonAsync<ApiPasswordRequirements>(JsonDefaults.Options);
        requirements.Should().NotBeNull();
        requirements!.MinimumLength.Should().Be(8);
        requirements.RequireDigit.Should().BeTrue();
        requirements.RequireUppercase.Should().BeTrue();
        requirements.RequireLowercase.Should().BeTrue();
        requirements.RequireNonAlphanumeric.Should().BeFalse();
    }

    private sealed record PasswordProblemResponse(
        int? Status,
        string? Title,
        string? Type,
        string? Detail,
        List<string>? FailedRequirements,
        List<string>? Messages,
        ApiPasswordRequirements? Requirements);

    [Fact]
    public async Task V2_GetAllUsers_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var part = new MusicPart { Id = Guid.NewGuid(), Name = "List test part", Indexable = true };

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            await db.MusicParts.AddAsync(part);
            await db.SaveChangesAsync();
        }

        var assignResponse = await client.PutAsJsonAsync($"users/{TestUser.Testesen.Identifier}/parts", new { PartIds = new[] { part.Id } });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var testesen = document.RootElement.EnumerateArray()
            .Single(user => user.GetProperty("id").GetGuid() == TestUser.Testesen.Identifier);
        var roles = testesen.GetProperty("roles").EnumerateArray().Select(role => role.GetString());
        roles.Should().BeEquivalentTo([Roles.Musikant, Roles.Arkivleser]);
        testesen.GetProperty("parts").EnumerateArray().Select(part => part.GetProperty("id").GetGuid()).Should().Equal(part.Id);
    }

    [Fact]
    public async Task V2_GetAllUsers_ShouldBeForbidden_WhenMusikant()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_GetAllUsers_ShouldBeForbidden_WhenNoteansvarlig()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_AssignRole_ShouldBeForbidden_WhenNoteansvarlig()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Testesen.Identifier}/roles", new { RoleName = "Admin" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_AssignRole_ShouldTakeEffectImmediately_WithoutReauthenticating()
    {
        var anonymousClient = CreateV2Client();
        var (id, email) = await RegisterInactiveUserAsync(anonymousClient, "role-takes-effect");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        await adminClient.PutAsJsonAsync($"users/{id}/activate", new { });

        // Roles are resolved per request rather than baked into the token, so the same credentials
        // must go from denied to allowed the moment the role is granted.
        var userClient = factory.CreateClientWithTestToken(new TestUser { Identifier = id, Email = email, Name = "role-takes-effect", Password = "SecurePassword123!" });

        (await userClient.GetAsync("parts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var assignResponse = await adminClient.PutAsJsonAsync($"users/{id}/roles", new { RoleName = "Noteansvarlig" });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await userClient.GetAsync("parts")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_AsMe_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_AsMe_ShouldBeSuccessful_WhenNonAdministrator()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_ById_ShouldBeForbidden_WhenNonAdminRequestsAnotherUser()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync($"users/{TestUser.Administrator.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("bullshit")]
    [InlineData("almost-a-guid-9FA3890E-D008-4791-B841-A1AD283BE86F")]
    public async Task V2_GetUser_WithInvalidIdentifier_ShouldGiveBadRequest(string identifier)
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await client.GetAsync($"users/{identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetUser_ById_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{TestUser.Administrator.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_ById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task V2_GetProfilePicture_ShouldReturnUnauthorized_WhenAnonymous()
    {
        var client = CreateV2Client();

        var response = await client.GetAsync($"users/{TestUser.Testesen.Identifier}/profile-picture");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldReturnForbidden_WhenUserUpdatesAnotherUser()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Musikant);

        var response = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldReturnBadRequest_WhenImageIsMalformed()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("not an image")), "file", "picture.png" },
            { new StringContent("0"), "x" },
            { new StringContent("0"), "y" },
            { new StringContent("1"), "size" }
        };

        var response = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        using var content = new MultipartFormDataContent
        {
            { new StringContent("0"), "x" },
            { new StringContent("0"), "y" },
            { new StringContent("1"), "size" }
        };

        var response = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldExposeNewVersionAndReturnWebp_WhenUserUpdatesOwnPicture()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var blobs = ConfigureProfilePictureBlobs();

        var upload = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());
        upload.StatusCode.Should().Be(HttpStatusCode.OK, await upload.Content.ReadAsStringAsync());
        var uploaded = await upload.Content.ReadFromJsonAsync<ApiProfilePicture>(JsonDefaults.Options);

        using var userDocument = JsonDocument.Parse(await client.GetStringAsync($"users/{TestUser.Testesen.Identifier}"));
        userDocument.RootElement.GetProperty("profilePicture").GetProperty("version").GetGuid().Should().Be(uploaded!.Version);

        var get = await client.GetAsync($"users/{TestUser.Testesen.Identifier}/profile-picture");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        get.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        get.Headers.ETag!.Tag.Should().Be($"\"{uploaded.Version:N}\"");
        blobs.Should().ContainSingle();
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldReplaceVersionAndDeleteSupersededBlob_WhenPictureAlreadyExists()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var blobs = ConfigureProfilePictureBlobs();

        var first = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());
        var firstPicture = await first.Content.ReadFromJsonAsync<ApiProfilePicture>(JsonDefaults.Options);
        var second = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());
        var secondPicture = await second.Content.ReadFromJsonAsync<ApiProfilePicture>(JsonDefaults.Options);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        secondPicture!.Version.Should().NotBe(firstPicture!.Version);
        blobs.Should().ContainSingle();
        blobs.Keys.Single().Should().Contain(secondPicture.Version.ToString("N"));
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldPersistReplacement_WhenSupersededBlobCleanupFails()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        ConfigureProfilePictureBlobs();
        var first = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());
        var firstPicture = await first.Content.ReadFromJsonAsync<ApiProfilePicture>(JsonDefaults.Options);
        factory.BlobMock.Setup(blobClient => blobClient.DeleteProfilePictureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Blob storage is unavailable"));

        var second = await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());
        var secondPicture = await second.Content.ReadFromJsonAsync<ApiProfilePicture>(JsonDefaults.Options);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        secondPicture!.Version.Should().NotBe(firstPicture!.Version);
    }

    [Fact]
    public async Task V2_UploadProfilePicture_ShouldKeepOnlyCurrentBlob_WhenUploadsAreConcurrent()
    {
        var firstClient = CreateV2ClientWithTestToken(TestUser.Testesen);
        var secondClient = CreateV2ClientWithTestToken(TestUser.Testesen);
        var blobs = ConfigureProfilePictureBlobs();

        var uploads = await Task.WhenAll(
            firstClient.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent()),
            secondClient.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent()));
        var responses = await Task.WhenAll(uploads.Select(response => response.Content.ReadAsStringAsync()));

        uploads.Select(response => response.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict], string.Join("; ", responses));
        blobs.Should().ContainSingle();
    }

    [Fact]
    public async Task V2_GetProfilePicture_ShouldReturnNotFound_WhenUserPictureBlobIsMissing()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        ConfigureProfilePictureBlobs();
        using (var scope = factory.TestServices.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(TestUser.Testesen.Identifier.ToString());
            user!.ProfilePictureBlobName = "profile-pictures/missing.webp";
            user.ProfilePictureVersion = Guid.NewGuid();
            (await userManager.UpdateAsync(user)).Succeeded.Should().BeTrue();
        }

        var response = await client.GetAsync($"users/{TestUser.Testesen.Identifier}/profile-picture");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task V2_RemoveProfilePicture_ShouldDeletePictureAndClearUserState_WhenUserRemovesOwnPicture()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var blobs = ConfigureProfilePictureBlobs();
        await client.PutAsync($"users/{TestUser.Testesen.Identifier}/profile-picture", CreateProfilePictureContent());

        var remove = await client.DeleteAsync($"users/{TestUser.Testesen.Identifier}/profile-picture");
        using var userDocument = JsonDocument.Parse(await client.GetStringAsync($"users/{TestUser.Testesen.Identifier}"));
        var get = await client.GetAsync($"users/{TestUser.Testesen.Identifier}/profile-picture");

        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);
        userDocument.RootElement.GetProperty("profilePicture").ValueKind.Should().Be(JsonValueKind.Null);
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldDeleteProfilePictureBlob_WhenHardDeletingUser()
    {
        var anonymousClient = CreateV2Client();
        var blobs = ConfigureProfilePictureBlobs();
        const string blobName = "profile-pictures/hard-delete/picture.webp";
        blobs[blobName] = [1, 2, 3];
        var (userId, _) = await RegisterInactiveUserAsync(anonymousClient, "hard-delete-profile-picture");
        using (var scope = factory.TestServices.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            user!.ProfilePictureBlobName = blobName;
            user.ProfilePictureVersion = Guid.NewGuid();
            (await userManager.UpdateAsync(user)).Succeeded.Should().BeTrue();
        }

        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await client.DeleteAsync($"users/{userId}?hardDelete=true");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task V2_AssignParts_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Musikant);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Testesen.Identifier}/parts", new { PartIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_AssignParts_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var client = CreateV2Client();

        var response = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task V2_AssignParts_ShouldReturnBadRequest_WhenPartIdsAreNull()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = (Guid[]?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_AssignParts_ShouldReturnNotFound_WhenPartDoesNotExist()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = new[] { Guid.NewGuid() } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task V2_AssignParts_ShouldReturnAssignedParts_WhenRetrievingUser()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var parts = new[]
        {
            new MusicPart { Id = Guid.NewGuid(), Name = "Part one", SortOrder = 2, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Part two", SortOrder = 1, Indexable = true }
        };

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            await db.MusicParts.AddRangeAsync(parts);
            await db.SaveChangesAsync();
        }

        var assignResponse = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = parts.Select(part => part.Id) });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await client.GetFromJsonAsync<ApiUserDetailModel>($"users/{TestUser.Musikant.Identifier}");
        user!.Parts.Select(part => part.Id).Should().Equal(parts.OrderBy(part => part.SortOrder).Select(part => part.Id));
    }

    [Fact]
    public async Task V2_AssignParts_ShouldReplaceExistingAssignments_WhenCalledAgain()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var parts = new[]
        {
            new MusicPart { Id = Guid.NewGuid(), Name = "Original part", Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Replacement part", Indexable = true }
        };

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            await db.MusicParts.AddRangeAsync(parts);
            await db.SaveChangesAsync();
        }

        (await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = new[] { parts[0].Id } })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = new[] { parts[1].Id } })).StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await client.GetFromJsonAsync<ApiUserDetailModel>($"users/{TestUser.Musikant.Identifier}");
        user!.Parts.Select(part => part.Id).Should().Equal(parts[1].Id);
    }

    [Fact]
    public async Task V2_AssignParts_ShouldClearExistingAssignments_WhenPartIdsAreEmpty()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var part = new MusicPart { Id = Guid.NewGuid(), Name = "Part to clear", Indexable = true };

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            await db.MusicParts.AddAsync(part);
            await db.SaveChangesAsync();
        }

        (await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = new[] { part.Id } })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}/parts", new { PartIds = Array.Empty<Guid>() })).StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await client.GetFromJsonAsync<ApiUserDetailModel>($"users/{TestUser.Musikant.Identifier}");
        user!.Parts.Should().BeEmpty();
    }

    [Fact]
    public async Task V2_AssignParts_ShouldNotReplaceExistingAssignments_WhenPartIdsAreDuplicated()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var (userId, _) = await RegisterInactiveUserAsync(CreateV2Client(), "duplicate-part-ids");
        var parts = new[]
        {
            new MusicPart { Id = Guid.NewGuid(), Name = "Existing part", Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Duplicate part", Indexable = true }
        };

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            await db.MusicParts.AddRangeAsync(parts);
            await db.SaveChangesAsync();
        }

        (await client.PutAsJsonAsync($"users/{userId}/parts", new { PartIds = new[] { parts[0].Id } })).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PutAsJsonAsync($"users/{userId}/parts", new { PartIds = new[] { parts[1].Id, parts[1].Id } });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var user = await client.GetFromJsonAsync<ApiUserDetailModel>($"users/{userId}");
        user!.Parts.Select(part => part.Id).Should().Equal(parts[0].Id);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldBeSuccessful_WhenAdminUpdatesAnother()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Administrator.Identifier}", new
        {
            Password = "UpdatedAdmin123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldPersistNameAndEmail_WhenProfileFieldsAreProvided()
    {
        var anonymousClient = CreateV2Client();
        var originalEmail = $"update-{Guid.NewGuid():N}@user.com";
        var updatedEmail = $"updated-{Guid.NewGuid():N}@user.com";
        var userId = Guid.NewGuid();

        var registerResponse = await anonymousClient.PostAsJsonAsync("users/register", new
        {
            Id = userId,
            Name = "Original User",
            Email = originalEmail,
            Password = "Original123!"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var updateResponse = await adminClient.PutAsJsonAsync($"users/{userId}", new
        {
            Name = "Updated User",
            Email = updatedEmail
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await adminClient.GetAsync($"users/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await getResponse.Content.ReadFromJsonAsync<UserResponse>(JsonDefaults.Options);
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be("Updated User");
        updatedUser.Email.Should().Be(updatedEmail);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var identityUser = await userManager.FindByIdAsync(userId.ToString());
        identityUser!.UserName.Should().Be(updatedEmail);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldReturnBadRequest_WhenEmailIsAlreadyInUse()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}", new
        {
            Email = TestUser.Administrator.Email
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Musikant.Identifier}", new
        {
            Email = "not-an-email"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Administrator.Identifier}", new
        {
            Password = "HackerPassword1!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldReturnBadRequest_WhenWeakPassword()
    {
        var anonymousClient = CreateV2Client();
        var email = $"weakupdate-{Guid.NewGuid():N}@user.com";
        const string originalPassword = "Original123!";

        await anonymousClient.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Weak Update User",
            Email = email,
            Password = originalPassword
        });

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);
        appUser!.Inactive = false;
        await userManager.UpdateAsync(appUser);

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.PutAsJsonAsync($"users/{appUser.Id}", new { Password = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<PasswordProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(nameof(PasswordRequirementsNotMetError));

        // The bug this guards against: UpdateUser used to discard the IdentityResult and return 200,
        // silently leaving the password unchanged while telling the caller it had been updated.
        var loginResponse = await anonymousClient.PostAsync("token", BuildLoginForm(email, originalPassword));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record ApiUserDetailModel(IReadOnlyList<ApiPartModel> Parts);

    private Dictionary<string, byte[]> ConfigureProfilePictureBlobs()
    {
        var blobs = new Dictionary<string, byte[]>();
        factory.BlobMock.Setup(client => client.AddProfilePictureAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<string, Stream, CancellationToken>(async (blobName, content, cancellationToken) =>
            {
                using var copy = new MemoryStream();
                await content.CopyToAsync(copy, cancellationToken);
                blobs.Add(blobName, copy.ToArray());
            });
        factory.BlobMock.Setup(client => client.GetProfilePictureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((blobName, _) => blobs.TryGetValue(blobName, out var content)
                ? Task.FromResult<Stream>(new MemoryStream(content))
                : Task.FromException<Stream>(new FileNotFoundException("Profile picture blob was not found", blobName)));
        factory.BlobMock.Setup(client => client.DeleteProfilePictureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((blobName, _) =>
            {
                blobs.Remove(blobName);
                return Task.CompletedTask;
            });
        return blobs;
    }

    private static MultipartFormDataContent CreateProfilePictureContent()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 0, 0));
        using var imageContent = new MemoryStream();
        image.SaveAsPng(imageContent);

        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageContent.ToArray()), "file", "picture.png");
        content.Add(new StringContent("0"), "x");
        content.Add(new StringContent("0"), "y");
        content.Add(new StringContent("1"), "size");
        return content;
    }

    private record ApiPartModel(Guid Id);

    // --- User management: activate / deactivate / roles / delete ---

    private static async Task<(Guid Id, string Email)> RegisterInactiveUserAsync(HttpClient client, string namePrefix)
    {
        var id = Guid.NewGuid();
        var email = $"{namePrefix}-{Guid.NewGuid():N}@user.com";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = id,
            Name = namePrefix,
            Email = email,
            Password = "SecurePassword123!"
        });

        return (id, email);
    }

    [Fact]
    public async Task V2_Register_ShouldAssignMusikantRole()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "default-role");

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id.ToString());

        (await userManager.GetRolesAsync(user!)).Should().BeEquivalentTo(["Musikant"]);
    }

    [Fact]
    public async Task V2_ActivateUser_ShouldAllowLogin_WhenAdmin()
    {
        var anonymousClient = CreateV2Client();
        var (id, email) = await RegisterInactiveUserAsync(anonymousClient, "activate");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var activateResponse = await adminClient.PutAsJsonAsync($"users/{id}/activate", new { });
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await anonymousClient.PostAsync("token", BuildLoginForm(email, "SecurePassword123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_ActivateUser_ShouldBeForbidden_WhenNonAdmin()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "activate-forbidden");

        var musikantClient = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await musikantClient.PutAsJsonAsync($"users/{id}/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_ActivateUser_ShouldBeForbidden_WhenNoteansvarlig()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "activate-forbidden-noteansvarlig");

        var noteansvarligClient = CreateV2ClientWithTestToken(TestUser.Noteansvarlig);
        var response = await noteansvarligClient.PutAsJsonAsync($"users/{id}/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_ActivateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await client.PutAsJsonAsync($"users/{Guid.NewGuid()}/activate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task V2_DeactivateUser_ShouldPreventLogin_WhenAdmin()
    {
        var anonymousClient = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(anonymousClient, password);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var deactivateResponse = await adminClient.PutAsJsonAsync($"users/{appUser!.Id}/deactivate", new { });
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await anonymousClient.PostAsync("token", BuildLoginForm(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_DeactivateUser_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.PutAsJsonAsync($"users/{TestUser.Administrator.Identifier}/deactivate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_AssignRole_ShouldAddRoleToUser_WhenAdmin()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "assign-role");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.PutAsJsonAsync($"users/{id}/roles", new { RoleName = "Admin" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id.ToString());
        (await userManager.IsInRoleAsync(user!, "Admin")).Should().BeTrue();
    }

    [Fact]
    public async Task V2_AssignRole_ShouldReturnBadRequest_WhenRoleIsNotAKnownRole()
    {
        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.PutAsJsonAsync($"users/{TestUser.Testesen.Identifier}/roles", new { RoleName = "NonExistentRole" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Noteansvarlig")]
    [InlineData("Musikant")]
    public async Task V2_AssignRole_ShouldAcceptRole_WhenRoleIsKnown(string roleName)
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, $"assign-{roleName.ToLowerInvariant()}");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.PutAsJsonAsync($"users/{id}/roles", new { RoleName = roleName });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id.ToString());
        (await userManager.IsInRoleAsync(user!, roleName)).Should().BeTrue();
    }

    [Fact]
    public async Task V2_AssignRole_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.PutAsJsonAsync($"users/{TestUser.Testesen.Identifier}/roles", new { RoleName = "Admin" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_RemoveRole_ShouldRemoveRoleFromUser_WhenAdmin()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "remove-role");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        await adminClient.PutAsJsonAsync($"users/{id}/roles", new { RoleName = "Admin" });

        var response = await adminClient.DeleteAsync($"users/{id}/roles/Admin");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(id.ToString());
        (await userManager.IsInRoleAsync(user!, "Admin")).Should().BeFalse();
    }

    [Fact]
    public async Task V2_RemoveRole_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"users/{TestUser.Testesen.Identifier}/roles/Musikant");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_RemoveRole_ShouldBeForbidden_WhenNoteansvarlig()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Noteansvarlig);
        var response = await client.DeleteAsync($"users/{TestUser.Testesen.Identifier}/roles/Musikant");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldSoftDelete_ByDefault()
    {
        var anonymousClient = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(anonymousClient, password);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.DeleteAsync($"users/{appUser!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await anonymousClient.PostAsync("token", BuildLoginForm(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var stillExists = await userManager.FindByIdAsync(appUser.Id.ToString());
        stillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldHardDelete_WhenRequested()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "hard-delete");

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.DeleteAsync($"users/{id}?hardDelete=true");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var deletedUser = await userManager.FindByIdAsync(id.ToString());
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldDetachConnectedMusician_WhenHardDeleting()
    {
        var anonymousClient = CreateV2Client();
        var (id, _) = await RegisterInactiveUserAsync(anonymousClient, "hard-delete-musician");

        Guid musicianId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var musician = new Musician { Id = Guid.NewGuid(), ApplicationUserId = id };
            db.Musicians.Add(musician);
            await db.SaveChangesAsync();
            musicianId = musician.Id;
        }

        var adminClient = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await adminClient.DeleteAsync($"users/{id}?hardDelete=true");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var musician = await db.Musicians.SingleAsync(m => m.Id == musicianId);
            musician.ApplicationUserId.Should().BeNull();
        }
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"users/{TestUser.Testesen.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await client.DeleteAsync($"users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- Forgot password / Password reset ---

    [Fact]
    public async Task ForgotPassword_ShouldReturn200AndSendEmail_WhenActiveUserExists()
    {
        var client = CreateV2Client();
        factory.FakeEmail.Clear();

        var response = await client.PostAsJsonAsync("users/forgot-password", new { Email = TestUser.Testesen.Email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.FakeEmail.SentEmails.Should().HaveCount(1);
        factory.FakeEmail.SentEmails[0].ToEmail.Should().Be(TestUser.Testesen.Email);
        factory.FakeEmail.SentEmails[0].ResetToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200AndSendNoEmail_WhenEmailDoesNotExist()
    {
        var client = CreateV2Client();
        factory.FakeEmail.Clear();

        var response = await client.PostAsJsonAsync("users/forgot-password", new { Email = "unknown@nobody.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.FakeEmail.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200AndSendNoEmail_WhenUserIsInactive()
    {
        var client = CreateV2Client();
        factory.FakeEmail.Clear();

        // Register an inactive user
        var email = $"inactive-fp-{Guid.NewGuid():N}@user.com";
        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Inactive FP User",
            Email = email,
            Password = "SecurePassword123!"
        });

        var response = await client.PostAsJsonAsync("users/forgot-password", new { Email = email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.FakeEmail.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn400_WhenEmailIsInvalid()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/forgot-password", new { Email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Use a brand-new factory (own in-memory database and rate limiter state) with a low
        // limit, so this test doesn't affect the shared factory/database used by other tests.
        using var limitedFactory = new SheetMusicWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:ForgotPassword:PermitLimit", "2");
            builder.UseSetting("RateLimiting:ForgotPassword:WindowSeconds", "60");
        });

        var client = limitedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");

        await client.PostAsJsonAsync("users/forgot-password", new { Email = "rate-limit-test@user.com" });
        await client.PostAsJsonAsync("users/forgot-password", new { Email = "rate-limit-test@user.com" });
        var response = await client.PostAsJsonAsync("users/forgot-password", new { Email = "rate-limit-test@user.com" });

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn200AndUpdatePassword_WhenTokenIsValid()
    {
        var client = CreateV2Client();
        factory.FakeEmail.Clear();

        // Register a dedicated user for this test to avoid mutating shared test users
        var email = $"resetpw-{Guid.NewGuid():N}@user.com";
        const string newPassword = "NewPassword999!";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Reset PW User",
            Email = email,
            Password = "Original123!"
        });

        // Activate the user directly through Identity (newly registered users are inactive)
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);
        appUser!.Inactive = false;
        await userManager.UpdateAsync(appUser);

        // Request a reset token
        await client.PostAsJsonAsync("users/forgot-password", new { Email = email });
        factory.FakeEmail.SentEmails.Should().HaveCount(1);
        var token = factory.FakeEmail.SentEmails[0].ResetToken;

        // Reset the password
        var resetResponse = await client.PostAsJsonAsync("users/reset-password", new
        {
            Email = email,
            Token = token,
            NewPassword = newPassword
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the new password works
        var loginResponse = await client.PostAsync("token", new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", email),
            new("password", newPassword)
        }));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WhenTokenIsInvalid()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/reset-password", new
        {
            Email = TestUser.Testesen.Email,
            Token = "invalid-token-value",
            NewPassword = "NewPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_ResetPassword_ShouldReturnPasswordError_WhenWeakPassword()
    {
        var client = CreateV2Client();
        factory.FakeEmail.Clear();

        // Register a dedicated user for this test to avoid mutating shared test users
        var email = $"resetweak-{Guid.NewGuid():N}@user.com";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Reset Weak User",
            Email = email,
            Password = "Original123!"
        });

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);
        appUser!.Inactive = false;
        await userManager.UpdateAsync(appUser);

        // Request a valid reset token
        await client.PostAsJsonAsync("users/forgot-password", new { Email = email });
        factory.FakeEmail.SentEmails.Should().HaveCount(1);
        var token = factory.FakeEmail.SentEmails[0].ResetToken;

        // A valid token plus a weak password must be reported as a password error, not an expired/invalid link
        var response = await client.PostAsJsonAsync("users/reset-password", new
        {
            Email = email,
            Token = token,
            NewPassword = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<PasswordProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(nameof(PasswordRequirementsNotMetError));
        problem.FailedRequirements.Should().Contain("PasswordTooShort");
    }

    [Fact]
    public async Task V2_ResetPassword_ShouldReturnGenericError_WhenUnknownEmail()
    {
        var client = CreateV2Client();

        // Guards the anti-enumeration behaviour: an unknown email must still get the generic
        // invalid-token error, never a password-specific one.
        var response = await client.PostAsJsonAsync("users/reset-password", new
        {
            Email = $"unknown-reset-{Guid.NewGuid():N}@nobody.com",
            Token = "irrelevant-token",
            NewPassword = "StrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<PasswordProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Type.Should().Be(nameof(InvalidPasswordResetTokenError));
        problem.FailedRequirements.Should().BeNull();
    }

    // --- Account lockout ---

    private async Task<string> RegisterAndActivateUserAsync(HttpClient client, string password)
    {
        var email = $"lockout-{Guid.NewGuid():N}@user.com";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Lockout Test User",
            Email = email,
            Password = password
        });

        // Activate the user directly through Identity (newly registered users are inactive)
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var appUser = await userManager.FindByEmailAsync(email);
        appUser!.Inactive = false;
        await userManager.UpdateAsync(appUser);

        return email;
    }

    private static FormUrlEncodedContent BuildLoginForm(string email, string password) => new(new List<KeyValuePair<string?, string?>>
    {
        new("grant_type", "basic"),
        new("username", email),
        new("password", password)
    });

    [Fact]
    public async Task V2_GetToken_ShouldLockAccount_AfterMaxFailedAttempts()
    {
        var client = CreateV2Client();
        const string correctPassword = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, correctPassword);

        // IdentityOptions.Lockout.MaxFailedAccessAttempts is configured to 5 in Program.cs
        for (var i = 0; i < 5; i++)
        {
            var failedResponse = await client.PostAsync("token", BuildLoginForm(email, "wrong-password"));
            failedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // Even with the correct password, the account should now be locked out.
        // The response intentionally uses the same generic message as invalid credentials
        // to avoid leaking lockout state to an unauthenticated caller.
        var response = await client.PostAsync("token", BuildLoginForm(email, correctPassword));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldNotLockAccount_BeforeMaxFailedAttemptsReached()
    {
        var client = CreateV2Client();
        const string correctPassword = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, correctPassword);

        for (var i = 0; i < 4; i++)
        {
            await client.PostAsync("token", BuildLoginForm(email, "wrong-password"));
        }

        var response = await client.PostAsync("token", BuildLoginForm(email, correctPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Refresh tokens ---

    private static FormUrlEncodedContent BuildRefreshForm(string refreshToken) => new(new List<KeyValuePair<string?, string?>>
    {
        new("grant_type", "refresh_token"),
        new("refresh_token", refreshToken)
    });

    [Fact]
    public async Task V2_GetToken_ShouldReturnRefreshToken_OnSuccessfulLogin()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        var response = await client.PostAsync("token", BuildLoginForm(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);
        tokens.Should().NotBeNull();
        tokens!.access_token.Should().NotBeNullOrWhiteSpace();
        tokens.refresh_token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldIssueNewTokenPair_WhenRefreshTokenIsValid()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        var loginResponse = await client.PostAsync("token", BuildLoginForm(email, password));
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);

        var refreshResponse = await client.PostAsync("token", BuildRefreshForm(loginTokens!.refresh_token));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshedTokens = await refreshResponse.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);
        refreshedTokens.Should().NotBeNull();
        refreshedTokens!.access_token.Should().NotBeNullOrWhiteSpace();
        refreshedTokens.refresh_token.Should().NotBeNullOrWhiteSpace();
        refreshedTokens.refresh_token.Should().NotBe(loginTokens.refresh_token);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldRevokeOldToken_PreventingReuse()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        var loginResponse = await client.PostAsync("token", BuildLoginForm(email, password));
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);

        // First use rotates the refresh token - the old one is revoked in the same operation.
        var firstRefresh = await client.PostAsync("token", BuildRefreshForm(loginTokens!.refresh_token));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Reusing the original (now-revoked) refresh token must fail.
        var secondRefresh = await client.PostAsync("token", BuildRefreshForm(loginTokens.refresh_token));
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldReturnBadRequest_WhenTokenIsUnknown()
    {
        var client = CreateV2Client();

        var response = await client.PostAsync("token", BuildRefreshForm("not-a-real-refresh-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldReturnBadRequest_WhenRefreshTokenIsMissing()
    {
        var client = CreateV2Client();

        var response = await client.PostAsync("token", new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "refresh_token")
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldReturnBadRequest_WhenTokenIsExpired()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        // Insert an already-expired refresh token directly, matching how AccessTokenFactory hashes
        // the raw value before persisting it, so the endpoint can look it up by its stored digest.
        const string rawToken = "expired-raw-refresh-token-value";
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user!.Id,
            Token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var response = await client.PostAsync("token", BuildRefreshForm(rawToken));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldReturnBadRequest_WhenUserIsDeactivated()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        var loginResponse = await client.PostAsync("token", BuildLoginForm(email, password));
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user!.Inactive = true;
            await userManager.UpdateAsync(user);
        }

        var response = await client.PostAsync("token", BuildRefreshForm(loginTokens!.refresh_token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldReturnBadRequest_WhenGrantTypeIsUnsupported()
    {
        var client = CreateV2Client();

        var response = await client.PostAsync("token", new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "client_credentials"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldReturnBadRequest_WhenUsernameIsMissing()
    {
        var client = CreateV2Client();

        var response = await client.PostAsync("token", new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("password", TestUser.Testesen.Password)
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RefreshToken_ShouldOnlyAllowOneWinner_WhenRedeemedConcurrently()
    {
        var client = CreateV2Client();
        const string password = "SecurePassword123!";
        var email = await RegisterAndActivateUserAsync(client, password);

        var loginResponse = await client.PostAsync("token", BuildLoginForm(email, password));
        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<ApiAccessTokens>(JsonDefaults.Options);

        // Fire two redemptions of the same refresh token concurrently. The conditional (atomic) revoke
        // in RefreshAccessToken.Handler must let exactly one of them succeed.
        var responses = await Task.WhenAll(
            client.PostAsync("token", BuildRefreshForm(loginTokens!.refresh_token)),
            client.PostAsync("token", BuildRefreshForm(loginTokens.refresh_token)));

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(1);
    }

    private sealed class UserResponse
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
