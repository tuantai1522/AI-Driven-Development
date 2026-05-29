using AppModules.Sample.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppModules.Abstractions;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
