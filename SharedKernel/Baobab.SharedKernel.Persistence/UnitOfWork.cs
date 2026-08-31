using Baobab.SharedKernel.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Baobab.SharedKernel.Persistence;

public class UnitOfWork<T>(T dbContext) : IUnitOfWork
    where T : DbContext
{
    private readonly T _dbContext = dbContext;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);
}