using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace VendSys.Infrastructure.Data;

/// <summary>Default <see cref="ISqlExecutor"/> that delegates to the underlying ADO.NET connection.</summary>
/// <remarks>
/// Uses the raw <see cref="SqlConnection"/> obtained from the EF Core <see cref="VendSysDbContext"/>
/// so that OUTPUT parameters are reliably propagated back to the caller after execution.
/// </remarks>
public sealed class EfSqlExecutor : ISqlExecutor
{
    private readonly VendSysDbContext _context;

    public EfSqlExecutor(VendSysDbContext context) => _context = context;

    /// <inheritdoc/>
    public async Task ExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        await _context.Database.OpenConnectionAsync();
        try
        {
            var connection = (SqlConnection)_context.Database.GetDbConnection();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _context.Database.CloseConnection();
        }
    }
}
