using DocFlow.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DocFlow.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentProcessingHistory> DocumentProcessingHistory => Set<DocumentProcessingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("documents");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(300);
            builder.Property(x => x.StoredFileName).IsRequired().HasMaxLength(300);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
            builder.Property(x => x.SizeBytes).IsRequired();
            builder.Property(x => x.Checksum).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(x => x.UploadedAtUtc).IsRequired();
            builder.Property(x => x.ProcessedAtUtc);
            builder.Property(x => x.FailedAtUtc);
            builder.Property(x => x.FailureReason).HasMaxLength(1000);
            builder.Property(x => x.RetryCount).IsRequired();
            builder.Property(x => x.MaxRetryCount).IsRequired();
            builder.Property(x => x.ExtractedTitle).HasMaxLength(300);
            builder.Property(x => x.ExtractedTextPreview).HasMaxLength(2000);
            builder.Property(x => x.PageCount);
            builder.Property(x => x.MetadataJson);

            builder.HasMany(x => x.History)
                .WithOne()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            var historyNavigation = builder.Metadata.FindNavigation(nameof(Document.History));
            historyNavigation?.SetField("_history");
            historyNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DocumentProcessingHistory>(builder =>
        {
            builder.ToTable("document_processing_history");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DocumentId).IsRequired();
            builder.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(50);
            builder.Property(x => x.ToStatus).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Reason).HasMaxLength(1000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}
