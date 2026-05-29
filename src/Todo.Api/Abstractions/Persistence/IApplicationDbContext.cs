using Microsoft.EntityFrameworkCore;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
