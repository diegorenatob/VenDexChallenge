using System.Data;
using Microsoft.Data.SqlClient;
using VendSys.Application.DTOs;
using VendSys.Application.Interfaces;
using VendSys.Infrastructure.Data;

namespace VendSys.Infrastructure.Repositories;

/// <summary>Persists DEX data by executing stored procedures via <see cref="ISqlExecutor"/>.</summary>
public sealed class DexRepository : IDexRepository
{
    private readonly ISqlExecutor _executor;

    public DexRepository(ISqlExecutor executor) => _executor = executor;

    /// <inheritdoc/>
    public async Task<int> SaveDexMeterAsync(DexMeterDto dto)
    {
        var machineParam = new SqlParameter("@Machine", SqlDbType.NVarChar, 1) { Value = dto.Machine };
        var dexDateTimeParam = new SqlParameter("@DEXDateTime", SqlDbType.DateTime2) { Value = dto.DexDateTime };
        var serialParam = new SqlParameter("@MachineSerialNumber", SqlDbType.NVarChar, 50) { Value = dto.MachineSerialNumber };
        var vendsParam = new SqlParameter("@ValueOfPaidVends", SqlDbType.Decimal) { Value = dto.ValueOfPaidVends, Precision = 10, Scale = 2 };
        var idOutParam = new SqlParameter("@DexMeterId", SqlDbType.Int) { Direction = ParameterDirection.Output };

        await _executor.ExecuteAsync(
            "EXEC [dbo].[SaveDEXMeter] @Machine, @DEXDateTime, @MachineSerialNumber, @ValueOfPaidVends, @DexMeterId OUTPUT",
            machineParam, dexDateTimeParam, serialParam, vendsParam, idOutParam);

        return (int)idOutParam.Value;
    }

    /// <inheritdoc/>
    public async Task SaveDexLaneMeterAsync(int dexMeterId, DexLaneMeterDto dto)
    {
        var meterIdParam = new SqlParameter("@DexMeterId", SqlDbType.Int) { Value = dexMeterId };
        var productParam = new SqlParameter("@ProductIdentifier", SqlDbType.NVarChar, 50) { Value = dto.ProductIdentifier };
        var priceParam = new SqlParameter("@Price", SqlDbType.Decimal) { Value = dto.Price, Precision = 10, Scale = 2 };
        var vendsParam = new SqlParameter("@NumberOfVends", SqlDbType.Int) { Value = dto.NumberOfVends };
        var salesParam = new SqlParameter("@ValueOfPaidSales", SqlDbType.Decimal) { Value = dto.ValueOfPaidSales, Precision = 10, Scale = 2 };

        await _executor.ExecuteAsync(
            "EXEC [dbo].[SaveDEXLaneMeter] @DexMeterId, @ProductIdentifier, @Price, @NumberOfVends, @ValueOfPaidSales",
            meterIdParam, productParam, priceParam, vendsParam, salesParam);
    }
}
