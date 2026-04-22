namespace VendSys.Application.DTOs;

/// <summary>Transfer object carrying parsed DEX lane data before persistence.</summary>
public sealed class DexLaneMeterDto
{
    /// <summary>Lane product identifier from the PA1 segment.</summary>
    public string ProductIdentifier { get; set; } = string.Empty;

    /// <summary>Lane price in dollars (PA1 cents ÷ 100).</summary>
    public decimal Price { get; set; }

    /// <summary>Number of vends from the PA2 segment.</summary>
    public int NumberOfVends { get; set; }

    /// <summary>Total value of paid sales in dollars (PA2 cents ÷ 100).</summary>
    public decimal ValueOfPaidSales { get; set; }
}
