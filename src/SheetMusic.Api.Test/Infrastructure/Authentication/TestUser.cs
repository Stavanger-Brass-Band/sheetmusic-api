using System;

namespace SheetMusic.Api.Test.Infrastructure.Authentication;

public class TestUser
{
    public Guid Identifier { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsAdministrator { get; set; }

    public string Password { get; set; } = null!;

    public static TestUser Testesen => new TestUser
    {
        Identifier = Guid.Parse("0BC48204-A46A-4781-9D2D-F9F70317445A"),
        Name = "Test Testesen",
        Email = "test@testesen.com",
        Password = "intgTest123",
        IsAdministrator = false
    };

    public static TestUser Administrator => new TestUser
    {
        Identifier = Guid.Parse("2A319F65-C533-45BB-BB93-11C4492770AF"),
        Name = "Arild Administrator",
        Email = "arild@administrator.com",
        Password = "intgTest123",
        IsAdministrator = true
    };
}
