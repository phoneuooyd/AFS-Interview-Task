using AFS_Interview_Task.Domain;
using Microsoft.EntityFrameworkCore;

namespace AFS_Interview_Task.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TranslationLog> TranslationLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TranslationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Translator).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InputText).IsRequired().HasMaxLength(500);
            // In a real application, consider truncating the input text if it can be huge,
            // but the requirements say 1-500 chars so 500 max length is appropriate.
            // Output text can be longer due to translation length.
            entity.Property(e => e.OutputText).HasMaxLength(2000);
        });
    }
}