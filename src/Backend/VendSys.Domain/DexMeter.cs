namespace VendSys.Domain;

public sealed class DexMeter
{
    public int DexMeterId { get; set; }
    public string Machine { get; set; } = string.Empty;
    public DateTime DEXDateTime { get; set; }
    public string MachineSerialNumber { get; set; } = string.Empty;
    public decimal ValueOfPaidVends { get; set; }
}
