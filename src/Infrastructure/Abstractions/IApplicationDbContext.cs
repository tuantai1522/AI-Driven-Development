using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Abstractions;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
