using System.Reflection;
using VendSys.Client.Application.Constants;
using VendSys.Client.Application.Interfaces;

namespace VendSys.Maui.Infrastructure.Services;

public sealed class DexFileService : IDexFileService
{
    private static readonly Dictionary<string, string> _resourceKeys = new()
    {
        [Machines.A] = "VendSys.Maui.Resources.Dex.MachineA.txt",
        [Machines.B] = "VendSys.Maui.Resources.Dex.MachineB.txt",
    };

    private readonly Assembly _assembly;

    public DexFileService(Assembly assembly) => _assembly = assembly;

    public string LoadDexFile(string machine)
    {
        if (!_resourceKeys.TryGetValue(machine, out var resourceKey))
            throw new InvalidOperationException($"No DEX resource registered for machine '{machine}'.");

        using var stream = _assembly.GetManifestResourceStream(resourceKey)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceKey}' not found in assembly.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
