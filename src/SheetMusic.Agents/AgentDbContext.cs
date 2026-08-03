using Microsoft.EntityFrameworkCore;

namespace SheetMusic.Agents;

public sealed class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options)
{
    public DbSet<AgentSet> Sets { get; set; } = null!;
    public DbSet<AgentCategory> Categories { get; set; } = null!;
    public DbSet<AgentSheetMusicCategory> SetCategories { get; set; } = null!;
    public DbSet<AgentProject> Projects { get; set; } = null!;
    public DbSet<AgentProjectSet> ProjectSets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentSet>().ToTable("SheetMusicSets");
        modelBuilder.Entity<AgentCategory>().ToTable("Categories");
        modelBuilder.Entity<AgentSheetMusicCategory>().ToTable("SheetMusicCategories");
        modelBuilder.Entity<AgentProject>().ToTable("Projects");
        modelBuilder.Entity<AgentProjectSet>().ToTable("ProjectSheetMusicSets");

        modelBuilder.Entity<AgentSet>().HasKey(set => set.Id);
        modelBuilder.Entity<AgentCategory>().HasKey(category => category.Id);
        modelBuilder.Entity<AgentSheetMusicCategory>().HasKey(join => join.Id);
        modelBuilder.Entity<AgentProject>().HasKey(project => project.Id);
        modelBuilder.Entity<AgentProjectSet>().HasKey(join => join.Id);
    }
}

public sealed class AgentSet
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
}

public sealed class AgentCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Inactive { get; set; }
}

public sealed class AgentSheetMusicCategory
{
    public Guid Id { get; set; }
    public Guid SheetMusicSetId { get; set; }
    public Guid CategoryId { get; set; }
    public string Source { get; set; } = "Human";
    public string? ModelVersion { get; set; }
    public string? PromptVersion { get; set; }
    public DateTimeOffset? SuggestedAt { get; set; }
}

public sealed class AgentProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class AgentProjectSet
{
    public Guid Id { get; set; }
    public Guid SheetMusicSetId { get; set; }
    public Guid ProjectId { get; set; }
}
