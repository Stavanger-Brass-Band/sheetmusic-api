using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;

namespace SheetMusic.Api.Database;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SheetMusicContext>();

        await SeedRolesAsync(services);
        await SeedAdminUserAsync(services);
        await SeedPartsAsync(db);
        await SeedSetsAsync(db);
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in new[] { "Admin", "Reader" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
            }
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<SheetMusicContext>();

        // Seed legacy UserGroups for backward compatibility
#pragma warning disable CS0612 // Type or member is obsolete
        var adminGroup = await db.UserGroups.FirstOrDefaultAsync(g => g.Name == "Admin");
        if (adminGroup is null)
        {
            adminGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Admin" };
            db.UserGroups.Add(adminGroup);

            var readerGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Reader" };
            db.UserGroups.Add(readerGroup);
            await db.SaveChangesAsync();
        }
#pragma warning restore CS0612

        var adminUser = await userManager.FindByEmailAsync("admin@localhost");
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@localhost",
                Email = "admin@localhost",
                DisplayName = "Dev Admin",
                Inactive = false,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");

            CreatePasswordHash("Admin123!", out byte[] passwordHash, out byte[] passwordSalt);

#pragma warning disable CS0612
            db.Musicians.Add(new Musician
            {
                Id = Guid.NewGuid(),
                Name = "Dev Admin",
                Email = "admin@localhost",
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Inactive = false,
                UserGroupId = adminGroup.Id,
                ApplicationUserId = adminUser.Id
            });
#pragma warning restore CS0612
            await db.SaveChangesAsync();
        }
    }

    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static async Task SeedPartsAsync(SheetMusicContext db)
    {
        if (await db.MusicParts.AnyAsync())
            return;

        var parts = new[]
        {
            new MusicPart { Id = Guid.NewGuid(), Name = "Soprano Cornet", SortOrder = 1, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Solo Cornet", SortOrder = 2, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Repiano Cornet", SortOrder = 3, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "2nd Cornet", SortOrder = 4, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "3rd Cornet", SortOrder = 5, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Flugel Horn", SortOrder = 6, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Solo Horn", SortOrder = 7, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "1st Horn", SortOrder = 8, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "2nd Horn", SortOrder = 9, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "1st Baritone", SortOrder = 10, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "2nd Baritone", SortOrder = 11, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "1st Trombone", SortOrder = 12, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "2nd Trombone", SortOrder = 13, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Bass Trombone", SortOrder = 14, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Euphonium", SortOrder = 15, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Eb Bass", SortOrder = 16, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Bb Bass", SortOrder = 17, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Percussion", SortOrder = 18, Indexable = true },
            new MusicPart { Id = Guid.NewGuid(), Name = "Conductor", SortOrder = 19, Indexable = true },
        };

        db.MusicParts.AddRange(parts);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSetsAsync(SheetMusicContext db)
    {
        if (await db.SheetMusicSets.AnyAsync())
            return;

        var parts = await db.MusicParts.ToListAsync();

        var sets = new[]
        {
            new SheetMusicSet(1, "Doyen March") { Composer = "Thomas Olsen", Arranger = null },
            new SheetMusicSet(2, "Blaze Away") { Composer = "Abe Holzmann", Arranger = "W. Rimmer" },
            new SheetMusicSet(3, "Hymne") { Composer = "Edvard Grieg", Arranger = "J. Hanssen" },
        };

        db.SheetMusicSets.AddRange(sets);
        await db.SaveChangesAsync();

        // Link a few parts to the first set
        var soloCornet = parts.First(p => p.Name == "Solo Cornet");
        var euphonium = parts.First(p => p.Name == "Euphonium");
        var conductor = parts.First(p => p.Name == "Conductor");

        db.SheetMusicParts.AddRange(
            new SheetMusicPart { Id = Guid.NewGuid(), SetId = sets[0].Id, MusicPartId = soloCornet.Id },
            new SheetMusicPart { Id = Guid.NewGuid(), SetId = sets[0].Id, MusicPartId = euphonium.Id },
            new SheetMusicPart { Id = Guid.NewGuid(), SetId = sets[0].Id, MusicPartId = conductor.Id }
        );

        await db.SaveChangesAsync();
    }
}
