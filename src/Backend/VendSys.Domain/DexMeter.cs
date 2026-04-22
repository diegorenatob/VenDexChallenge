namespace VendSys.Domain;

/// <summary>Represents a DEX meter reading for one machine submission.</summary>
public sealed class DexMeter
{
    /// <summary>Auto-generated primary key.</summary>
    public int DexMeterId { get; set; }

    /// <summary>Machine label — "A" or "B".</summary>
    public string Machine { get; set; } = string.Empty;

    /// <summary>Timestamp extracted from the ID5 segment.</summary>
    public DateTime DEXDateTime { get; set; }

    /// <summary>Machine serial number from the ID1 segment.</summary>
    public string MachineSerialNumber { get; set; } = string.Empty;

    /// <summary>Total value of paid vends in dollars (VA1 cents ÷ 100).</summary>
    public decimal ValueOfPaidVends { get; set; }
}
