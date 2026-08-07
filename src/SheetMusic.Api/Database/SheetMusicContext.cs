using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts;

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
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectSheetMusicSet> ProjectSheetMusicSets { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SheetMusicCategory>()
            .HasOne(e => e.SheetMusicSet)
            .WithMany(e => e.Categories)
            .HasForeignKey(e => e.SheetMusicSetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SheetMusicPart>().Property(part => part.Source).HasDefaultValue("Human");
        modelBuilder.Entity<SheetMusicCategory>().Property(category => category.Source).HasDefaultValue("Human");

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Musician)
            .WithOne(m => m.ApplicationUser)
            .HasForeignKey<Musician>(m => m.ApplicationUserId);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // Optimistic concurrency guard used by RefreshAccessToken.Handler: two concurrent requests
        // redeeming the same refresh token both load it with RevokedAt == null, but only the first
        // SaveChangesAsync succeeds - the second sees a stale original value and throws
        // DbUpdateConcurrencyException, so a token can never be revoked/rotated more than once.
        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.RevokedAt)
            .IsConcurrencyToken();
    }
}
