using VendSys.Application.DTOs;

namespace VendSys.Application.Interfaces;

/// <summary>Parses a raw DEX text body into a <see cref="DexDocument"/>.</summary>
public interface IDexParserService
{
    /// <summary>
    /// Parses <paramref name="dexText"/> and returns the extracted meter and lane data.
    /// </summary>
    /// <param name="dexText">The raw DEX file content.</param>
    /// <returns>A <see cref="DexDocument"/> containing meter and lane readings.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dexText"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a required DEX segment is missing.</exception>
    ValueTask<DexDocument> ParseAsync(string dexText);
}
