using DocFlow.Application.Abstractions;
using DocFlow.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Persistence;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        await _dbContext.Documents.AddAsync(document, ct);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Documents
            .Include(x => x.History)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<Document>> GetPagedAsync(int skip, int take, DocumentStatus? status, CancellationToken ct = default)
    {
        var query = _dbContext.Documents.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.UploadedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(DocumentStatus? status, CancellationToken ct = default)
    {
        var query = _dbContext.Documents.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query.CountAsync(ct);
    }
}
