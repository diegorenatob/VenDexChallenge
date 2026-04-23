using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace VendSys.Infrastructure.Data;

/// <summary>Default <see cref="ISqlExecutor"/> that delegates to EF Core's raw-SQL API.</summary>
public sealed class EfSqlExecutor : ISqlExecutor
{
    private readonly VenDexDbContext _context;

    public EfSqlExecutor(VenDexDbContext context) => _context = context;

    /// <inheritdoc/>
    public Task ExecuteAsync(string sql, params SqlParameter[] parameters) =>
        _context.Database.ExecuteSqlRawAsync(sql, (object[])parameters);
}
