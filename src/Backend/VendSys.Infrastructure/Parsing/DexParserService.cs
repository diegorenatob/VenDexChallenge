using System.Globalization;
using VendSys.Application.DTOs;
using VendSys.Application.Interfaces;

namespace VendSys.Infrastructure.Parsing;

/// <summary>Parses raw DEX file text into a <see cref="DexDocument"/>.</summary>
public sealed class DexParserService : IDexParserService
{
    /// <inheritdoc/>
    public Task<DexDocument> ParseAsync(string dexText)
    {
        if (string.IsNullOrWhiteSpace(dexText))
            throw new ArgumentException("DEX text must not be null or whitespace.", nameof(dexText));

        var lines = dexText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        string? machineSerialNumber = null;
        DateTime? dexDateTime = null;
        decimal? valueOfPaidVends = null;
        var lanes = new List<DexLaneMeterDto>();
        DexLaneMeterDto? currentLane = null;

        foreach (var line in lines)
        {
            var fields = line.Split('*');
            switch (fields[0])
            {
                case "ID1":
                    machineSerialNumber = fields[1];
                    break;

                case "ID5":
                    // fields[1] = YYYYMMDD, fields[2] = HHMM
                    dexDateTime = DateTime.ParseExact(
                        fields[1] + fields[2],
                        "yyyyMMddHHmm",
                        CultureInfo.InvariantCulture);
                    break;

                case "VA1":
                    valueOfPaidVends = decimal.Parse(fields[1], CultureInfo.InvariantCulture) / 100m;
                    break;

                case "PA1":
                    currentLane = new DexLaneMeterDto
                    {
                        ProductIdentifier = fields[1],
                        Price = decimal.Parse(fields[2], CultureInfo.InvariantCulture) / 100m
                    };
                    lanes.Add(currentLane);
                    break;

                case "PA2":
                    if (currentLane is not null)
                    {
                        currentLane.NumberOfVends = int.Parse(fields[1], CultureInfo.InvariantCulture);
                        currentLane.ValueOfPaidSales = decimal.Parse(fields[2], CultureInfo.InvariantCulture) / 100m;
                        // one PA2 per PA1 block; subsequent PA2 lines (if any) are ignored
                        currentLane = null;
                    }
                    break;
            }
        }

        if (machineSerialNumber is null)
            throw new InvalidOperationException("Required DEX segment 'ID1' is missing.");
        if (dexDateTime is null)
            throw new InvalidOperationException("Required DEX segment 'ID5' is missing.");
        if (valueOfPaidVends is null)
            throw new InvalidOperationException("Required DEX segment 'VA1' is missing.");

        return Task.FromResult(new DexDocument
        {
            Meter = new DexMeterDto
            {
                MachineSerialNumber = machineSerialNumber,
                DexDateTime = dexDateTime.Value,
                ValueOfPaidVends = valueOfPaidVends.Value
            },
            Lanes = lanes
        });
    }
}
