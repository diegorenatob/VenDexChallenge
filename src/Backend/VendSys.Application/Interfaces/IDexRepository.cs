using VendSys.Application.DTOs;

namespace VendSys.Application.Interfaces;

/// <summary>Persists DEX meter and lane data via stored procedures.</summary>
public interface IDexRepository
{
    /// <summary>
    /// Upserts a DEX meter row and returns its database identifier.
    /// </summary>
    /// <param name="dto">The meter data to persist.</param>
    /// <returns>The <c>DexMeterId</c> of the inserted or updated row.</returns>
    Task<int> SaveDexMeterAsync(DexMeterDto dto);

    /// <summary>
    /// Inserts one DEX lane meter row linked to the given meter.
    /// </summary>
    /// <param name="dexMeterId">The parent meter identifier returned by <see cref="SaveDexMeterAsync"/>.</param>
    /// <param name="dto">The lane data to persist.</param>
    Task SaveDexLaneMeterAsync(int dexMeterId, DexLaneMeterDto dto);

    /// <summary>Deletes all rows from DEXLaneMeter and DEXMeter and reseeds their identity columns.</summary>
    Task ClearAllDataAsync();
}
