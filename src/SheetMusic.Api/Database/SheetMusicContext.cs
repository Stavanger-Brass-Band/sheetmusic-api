using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database.Entities;

namespace SheetMusic.Api.Database
{
    public class SheetMusicContext : DbContext
    {
        public SheetMusicContext(DbContextOptions<SheetMusicContext> options) : base(options)
        {
        }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SheetMusicCategory>()
                .HasOne(e => e.SheetMusicSet)
                .WithMany(e => e.Categories)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
