using System.Reflection;

namespace VendSys.Maui.Services;

public sealed class DexFileService : IDexFileService
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    private static readonly Dictionary<string, string> _resourceKeys = new()
    {
        [ApiConstants.MachineA] = "VendSys.Maui.Resources.Dex.MachineA.txt",
        [ApiConstants.MachineB] = "VendSys.Maui.Resources.Dex.MachineB.txt",
    };

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
