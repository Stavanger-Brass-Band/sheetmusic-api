using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Repositories;

namespace SheetMusic.Api.Database;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SheetMusicContext>();

        await SeedAdminUserAsync(db, services);
        await SeedPartsAsync(db);
        await SeedSetsAsync(db);
    }

    private static async Task SeedAdminUserAsync(SheetMusicContext db, IServiceProvider services)
    {
        var adminGroup = await db.UserGroups.FirstOrDefaultAsync(g => g.Name == "Admin");
        if (adminGroup is null)
        {
            adminGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Admin" };
            db.UserGroups.Add(adminGroup);
            await db.SaveChangesAsync();
        }

        if (!await db.Musicians.AnyAsync(m => m.UserGroupId == adminGroup.Id))
        {
            var userRepo = services.GetRequiredService<IUserRepository>();
            await userRepo.CreateAsync(new Musician
            {
                Id = Guid.NewGuid(),
                Name = "Dev Admin",
                Email = "admin@localhost",
                Inactive = false,
                UserGroupId = adminGroup.Id
            }, "admin");
        }
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
