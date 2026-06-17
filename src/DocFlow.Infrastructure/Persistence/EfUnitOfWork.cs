using DocFlow.Application.Abstractions;
using DocFlow.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public EfUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await NormalizeHistoryTrackingAsync(ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task NormalizeHistoryTrackingAsync(CancellationToken ct)
    {
        var modifiedHistoryEntries = _dbContext.ChangeTracker
            .Entries<DocumentProcessingHistory>()
            .Where(entry => entry.State == EntityState.Modified)
            .ToList();

        foreach (var entry in modifiedHistoryEntries)
        {
            var exists = await _dbContext.DocumentProcessingHistory
                .AsNoTracking()
                .AnyAsync(history => history.Id == entry.Entity.Id, ct);

            if (!exists)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
