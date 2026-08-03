using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Test.Infrastructure.Authentication;

public class TestUser
{
    public Guid Identifier { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsAdministrator { get; set; }

    public string Password { get; set; } = null!;

    public ApplicationUser AsApplicationUser() => new()
    {
        Id = Identifier,
        UserName = Email,
        Email = Email,
        DisplayName = Name,
        Inactive = false
    };

    public static TestUser Testesen => new TestUser
    {
        Identifier = Guid.Parse("0BC48204-A46A-4781-9D2D-F9F70317445A"),
        Name = "Test Testesen",
        Email = "test@testesen.com",
        Password = "IntgTest123!",
        IsAdministrator = false
    };

    public static TestUser Noteansvarlig => new TestUser
    {
        Identifier = Guid.Parse("6E0A1B6C-6C1D-4B0E-9A2F-3F1D2C4B5A67"),
        Name = "Nora Noteansvarlig",
        Email = "nora@noteansvarlig.com",
        Password = "IntgTest123!",
        IsAdministrator = false
    };

    public static TestUser Administrator => new TestUser
    {
        Identifier = Guid.Parse("2A319F65-C533-45BB-BB93-11C4492770AF"),
        Name = "Arild Administrator",
        Email = "arild@administrator.com",
        Password = "IntgTest123!",
        IsAdministrator = true
    };

    public static TestUser Prosjektleder => new TestUser
    {
        Identifier = Guid.Parse("8F3C6A1E-2D4B-4C5A-9E6F-7A1B2C3D4E5F"),
        Name = "Petter Prosjektleder",
        Email = "petter@prosjektleder.com",
        Password = "IntgTest123!",
        IsAdministrator = false
    };
}
