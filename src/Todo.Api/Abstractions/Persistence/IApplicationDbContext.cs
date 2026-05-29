using Microsoft.EntityFrameworkCore;
using Todo.Api.Domain.Users;
using Todo.Api.Persistence.Models;

namespace Todo.Api.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<SampleItem> SampleItems { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
