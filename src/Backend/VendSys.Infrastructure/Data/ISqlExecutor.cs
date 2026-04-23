using Microsoft.Data.SqlClient;

namespace VendSys.Infrastructure.Data;

/// <summary>Executes raw SQL commands against the database.</summary>
public interface ISqlExecutor
{
    /// <summary>Executes <paramref name="sql"/> with the supplied <paramref name="parameters"/>.</summary>
    Task ExecuteAsync(string sql, params SqlParameter[] parameters);
}
