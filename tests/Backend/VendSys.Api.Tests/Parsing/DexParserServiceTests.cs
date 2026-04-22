using System.Reflection;
using VendSys.Infrastructure.Parsing;

namespace VendSys.Api.Tests.Parsing;

[TestFixture]
public class DexParserServiceTests
{
    private static string _machineAText = string.Empty;
    private static string _machineBText = string.Empty;
    private DexParserService _sut = null!;

    [OneTimeSetUp]
    public static void LoadDexFiles()
    {
        _machineAText = LoadEmbedded("MachineA.txt");
        _machineBText = LoadEmbedded("MachineB.txt");
    }

    [SetUp]
    public void SetUp() => _sut = new DexParserService();

    private static string LoadEmbedded(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"VendSys.Api.Tests.TestData.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string RemoveLine(string text, string segmentPrefix) =>
        string.Join("\n", text.Split('\n').Where(l => !l.StartsWith(segmentPrefix)));

    // ── Machine A ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ParseAsync_MachineA_ReturnsMachineSerialNumber()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        Assert.That(doc.Meter.MachineSerialNumber, Is.EqualTo("100077238"));
    }

    [Test]
    public async Task ParseAsync_MachineA_ReturnsDexDateTime()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        Assert.That(doc.Meter.DexDateTime, Is.EqualTo(new DateTime(2023, 12, 10, 23, 10, 0)));
    }

    [Test]
    public async Task ParseAsync_MachineA_ReturnsValueOfPaidVends()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        Assert.That(doc.Meter.ValueOfPaidVends, Is.EqualTo(344.50m));
    }

    [Test]
    public async Task ParseAsync_MachineA_ReturnsCorrectLaneCount()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        Assert.That(doc.Lanes.Count, Is.EqualTo(38));
    }

    [Test]
    public async Task ParseAsync_MachineA_Lane101_ReturnsPrice()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        var lane = doc.Lanes.First(l => l.ProductIdentifier == "101");
        Assert.That(lane.Price, Is.EqualTo(3.25m));
    }

    [Test]
    public async Task ParseAsync_MachineA_Lane101_ReturnsNumberOfVends()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        var lane = doc.Lanes.First(l => l.ProductIdentifier == "101");
        Assert.That(lane.NumberOfVends, Is.EqualTo(4));
    }

    [Test]
    public async Task ParseAsync_MachineA_Lane101_ReturnsValueOfPaidSales()
    {
        var doc = await _sut.ParseAsync(_machineAText);
        var lane = doc.Lanes.First(l => l.ProductIdentifier == "101");
        Assert.That(lane.ValueOfPaidSales, Is.EqualTo(13.00m));
    }

    // ── Machine B ─────────────────────────────────────────────────────────────

    [Test]
    public async Task ParseAsync_MachineB_ReturnsMachineSerialNumber()
    {
        var doc = await _sut.ParseAsync(_machineBText);
        Assert.That(doc.Meter.MachineSerialNumber, Is.EqualTo("302029479"));
    }

    [Test]
    public async Task ParseAsync_MachineB_ReturnsDexDateTime()
    {
        var doc = await _sut.ParseAsync(_machineBText);
        Assert.That(doc.Meter.DexDateTime, Is.EqualTo(new DateTime(2023, 12, 10, 23, 11, 0)));
    }

    [Test]
    public async Task ParseAsync_MachineB_ReturnsValueOfPaidVends()
    {
        var doc = await _sut.ParseAsync(_machineBText);
        Assert.That(doc.Meter.ValueOfPaidVends, Is.EqualTo(4758.85m));
    }

    // ── Missing Segments ──────────────────────────────────────────────────────

    [Test]
    public void ParseAsync_MissingID1Segment_ThrowsInvalidOperationException()
    {
        var text = RemoveLine(_machineAText, "ID1");
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ParseAsync(text));
        Assert.That(ex!.Message, Does.Contain("ID1"));
    }

    [Test]
    public void ParseAsync_MissingVA1Segment_ThrowsInvalidOperationException()
    {
        var text = RemoveLine(_machineAText, "VA1");
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ParseAsync(text));
        Assert.That(ex!.Message, Does.Contain("VA1"));
    }

    [Test]
    public void ParseAsync_MissingID5Segment_ThrowsInvalidOperationException()
    {
        var text = RemoveLine(_machineAText, "ID5");
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ParseAsync(text));
        Assert.That(ex!.Message, Does.Contain("ID5"));
    }

    // ── Malformed Values ──────────────────────────────────────────────────────

    [Test]
    public void ParseAsync_EmptyBody_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ParseAsync(string.Empty));
    }

    [Test]
    public void ParseAsync_WhitespaceBody_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ParseAsync("   "));
    }

    [Test]
    public void ParseAsync_MalformedID5Date_ThrowsFormatException()
    {
        var text =
            "DXS*STF0000000*VA*V0/6*1\n" +
            "ID1*100077238*187**X**Y*6*1\n" +
            "ID5*BADDATE*2310*53*\n" +
            "VA1*34450*0*0*0*0*0*0*0*0*0*0*0\n";

        Assert.ThrowsAsync<FormatException>(() => _sut.ParseAsync(text));
    }

    [Test]
    public void ParseAsync_PA2WithNonNumericVends_ThrowsFormatException()
    {
        var text =
            "DXS*STF0000000*VA*V0/6*1\n" +
            "ID1*100077238*187**X**Y*6*1\n" +
            "ID5*20231210*2310*53*\n" +
            "VA1*34450*0*0*0*0*0*0*0*0*0*0*0\n" +
            "PA1*101*325*101****0**\n" +
            "PA2*abc*1300*0*0*0*0*0*0*0*0*0*0\n";

        Assert.ThrowsAsync<FormatException>(() => _sut.ParseAsync(text));
    }
}
