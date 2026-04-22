using VendSys.Application.DTOs;
using VendSys.Application.Interfaces;

namespace VendSys.Application.UseCases;

/// <summary>Result returned by <see cref="ProcessDexFileUseCase"/> after a successful submission.</summary>
public sealed record ProcessDexFileResult(
    string Machine,
    string SerialNumber,
    DateTime DexDateTime,
    decimal ValueOfPaidVends,
    int LanesProcessed);

/// <summary>Orchestrates DEX file parsing and persistence for one machine submission.</summary>
public sealed class ProcessDexFileUseCase
{
    private readonly IDexParserService _parser;
    private readonly IDexRepository _repository;

    public ProcessDexFileUseCase(IDexParserService parser, IDexRepository repository)
    {
        _parser = parser;
        _repository = repository;
    }

    /// <summary>Parses <paramref name="dexText"/>, persists the data, and returns a summary.</summary>
    public async Task<ProcessDexFileResult> ExecuteAsync(string dexText, string machine)
    {
        var document = await _parser.ParseAsync(dexText);
        document.Meter.Machine = machine;

        var dexMeterId = await _repository.SaveDexMeterAsync(document.Meter);

        foreach (var lane in document.Lanes)
            await _repository.SaveDexLaneMeterAsync(dexMeterId, lane);

        return new ProcessDexFileResult(
            machine,
            document.Meter.MachineSerialNumber,
            document.Meter.DexDateTime,
            document.Meter.ValueOfPaidVends,
            document.Lanes.Count);
    }
}
