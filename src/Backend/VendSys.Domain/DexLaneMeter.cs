namespace VendSys.Domain;

public sealed class DexLaneMeter
{
    public int DexLaneMeterId { get; set; }
    public int DexMeterId { get; set; }
    public string ProductIdentifier { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int NumberOfVends { get; set; }
    public decimal ValueOfPaidSales { get; set; }
}
