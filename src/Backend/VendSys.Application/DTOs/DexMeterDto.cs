namespace VendSys.Application.DTOs;

/// <summary>Transfer object carrying parsed DEX meter data before persistence.</summary>
public sealed record DexMeterDto
{
    /// <summary>Machine label — "A" or "B" — supplied by the API caller.</summary>
    public string Machine { get; init; } = string.Empty;

    /// <summary>Timestamp extracted from the ID5 segment.</summary>
    public DateTime DexDateTime { get; init; }

    /// <summary>Machine serial number from the ID1 segment.</summary>
    public string MachineSerialNumber { get; init; } = string.Empty;

    /// <summary>Total value of paid vends in dollars.</summary>
    public decimal ValueOfPaidVends { get; init; }
}
