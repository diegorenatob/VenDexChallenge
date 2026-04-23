using System.Globalization;
using VendSys.Application.DTOs;
using VendSys.Application.Interfaces;

namespace VendSys.Infrastructure.Parsing;

/// <summary>Parses raw DEX file text into a <see cref="DexDocument"/>.</summary>
public sealed class DexParserService : IDexParserService
{
    /// <inheritdoc/>
    public ValueTask<DexDocument> ParseAsync(string dexText)
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
                    if (fields.Length < 2)
                        throw new InvalidOperationException("Malformed 'ID1' segment: expected at least 2 fields.");
                    machineSerialNumber = fields[1];
                    break;

                case "ID5":
                    // fields[1] = YYYYMMDD, fields[2] = HHMM
                    if (fields.Length < 3)
                        throw new InvalidOperationException("Malformed 'ID5' segment: expected at least 3 fields.");
                    dexDateTime = DateTime.ParseExact(
                        fields[1] + fields[2],
                        "yyyyMMddHHmm",
                        CultureInfo.InvariantCulture);
                    break;

                case "VA1":
                    if (fields.Length < 2)
                        throw new InvalidOperationException("Malformed 'VA1' segment: expected at least 2 fields.");
                    valueOfPaidVends = decimal.Parse(fields[1], CultureInfo.InvariantCulture) / 100m;
                    break;

                case "PA1":
                    if (fields.Length < 3)
                        throw new InvalidOperationException("Malformed 'PA1' segment: expected at least 3 fields.");
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
                        if (fields.Length < 3)
                            throw new InvalidOperationException("Malformed 'PA2' segment: expected at least 3 fields.");
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

        return ValueTask.FromResult(new DexDocument
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
