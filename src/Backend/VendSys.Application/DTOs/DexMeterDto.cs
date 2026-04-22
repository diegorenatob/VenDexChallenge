namespace VendSys.Application.DTOs;

/// <summary>Transfer object carrying parsed DEX meter data before persistence.</summary>
public sealed class DexMeterDto
{
    /// <summary>Machine label — "A" or "B" — supplied by the API caller.</summary>
    public string Machine { get; set; } = string.Empty;

    /// <summary>Timestamp extracted from the ID5 segment.</summary>
    public DateTime DexDateTime { get; set; }

    /// <summary>Machine serial number from the ID1 segment.</summary>
    public string MachineSerialNumber { get; set; } = string.Empty;

    /// <summary>Total value of paid vends in dollars.</summary>
    public decimal ValueOfPaidVends { get; set; }
}
