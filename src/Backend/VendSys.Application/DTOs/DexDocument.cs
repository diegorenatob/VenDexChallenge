namespace VendSys.Application.DTOs;

/// <summary>The complete parse result of one DEX file submission.</summary>
public sealed class DexDocument
{
    /// <summary>Meter-level data extracted from the ID1, ID5, and VA1 segments.</summary>
    public DexMeterDto Meter { get; set; } = new();

    /// <summary>One entry per PA1/PA2 segment pair found in the file.</summary>
    public List<DexLaneMeterDto> Lanes { get; set; } = [];
}
