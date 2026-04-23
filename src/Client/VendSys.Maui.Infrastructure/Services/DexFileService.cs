using System.Reflection;
using VendSys.Client.Application.Constants;
using VendSys.Client.Application.Interfaces;

namespace VendSys.Maui.Infrastructure.Services;

public sealed class DexFileService : IDexFileService
{
    private readonly Assembly _assembly;
    private readonly string _prefix;

    public DexFileService(Assembly assembly)
    {
        _assembly = assembly;
        _prefix = assembly.GetName().Name + ".Resources.Dex.";
    }

    private static readonly IReadOnlyDictionary<string, string> _fileNames = new Dictionary<string, string>
    {
        [Machines.A] = "MachineA.txt",
        [Machines.B] = "MachineB.txt",
    };

    public string LoadDexFile(string machine)
    {
        if (!_fileNames.TryGetValue(machine, out var fileName))
            throw new InvalidOperationException($"No DEX resource registered for machine '{machine}'.");

        var resourceKey = _prefix + fileName;
        using var stream = _assembly.GetManifestResourceStream(resourceKey)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceKey}' not found in assembly.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
