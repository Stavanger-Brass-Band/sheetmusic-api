using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;

namespace SheetMusic.Api.Database;

public class SheetMusicContext(DbContextOptions<SheetMusicContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<SheetMusicSet> SheetMusicSets { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<SheetMusicCategory> SheetMusicCategories { get; set; } = null!;
    public DbSet<SheetMusicPart> SheetMusicParts { get; set; } = null!;
    public DbSet<MusicPart> MusicParts { get; set; } = null!;
    public DbSet<MusicPartAlias> MusicPartAliases { get; set; } = null!;
    public DbSet<Musician> Musicians { get; set; } = null!;
    public DbSet<UserGroup> UserGroups { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectSheetMusicSet> ProjectSheetMusicSets { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SheetMusicCategory>()
            .HasOne(e => e.SheetMusicSet)
            .WithMany(e => e.Categories)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Musician)
            .WithOne(m => m.ApplicationUser)
            .HasForeignKey<Musician>(m => m.ApplicationUserId);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
    }
}
