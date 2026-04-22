using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendSys.Application.DTOs;
using VendSys.Infrastructure.Data;
using VendSys.Infrastructure.Repositories;

namespace VendSys.Api.Tests.Repository;

[TestFixture]
public class DexRepositoryTests
{
    private CapturingDexRepository _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new CapturingDexRepository();

    // ── SaveDexMeterAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task SaveDexMeterAsync_ExecutesCorrectStoredProcedureName()
    {
        await _sut.SaveDexMeterAsync(BuildMeterDto());
        Assert.That(_sut.CapturedSql, Does.Contain("SaveDEXMeter"));
    }

    [Test]
    public async Task SaveDexMeterAsync_PassesMachineParameter()
    {
        await _sut.SaveDexMeterAsync(BuildMeterDto(machine: "A"));
        var param = GetParam("@Machine");
        Assert.That(param.Value, Is.EqualTo("A"));
    }

    [Test]
    public async Task SaveDexMeterAsync_PassesDexDateTimeParameter()
    {
        var dt = new DateTime(2023, 12, 10, 23, 10, 0);
        await _sut.SaveDexMeterAsync(BuildMeterDto(dexDateTime: dt));
        var param = GetParam("@DEXDateTime");
        Assert.That(param.Value, Is.EqualTo(dt));
    }

    [Test]
    public async Task SaveDexMeterAsync_PassesMachineSerialNumberParameter()
    {
        await _sut.SaveDexMeterAsync(BuildMeterDto(serial: "100077238"));
        var param = GetParam("@MachineSerialNumber");
        Assert.That(param.Value, Is.EqualTo("100077238"));
    }

    [Test]
    public async Task SaveDexMeterAsync_PassesValueOfPaidVendsParameter()
    {
        await _sut.SaveDexMeterAsync(BuildMeterDto(vends: 344.50m));
        var param = GetParam("@ValueOfPaidVends");
        Assert.That(param.Value, Is.EqualTo(344.50m));
    }

    [Test]
    public async Task SaveDexMeterAsync_ReadsOutputParameterAsReturnValue()
    {
        _sut.OutputValue = 42;
        var result = await _sut.SaveDexMeterAsync(BuildMeterDto());
        Assert.That(result, Is.EqualTo(42));
    }

    // ── SaveDexLaneMeterAsync ─────────────────────────────────────────────────

    [Test]
    public async Task SaveDexLaneMeterAsync_ExecutesCorrectStoredProcedureName()
    {
        await _sut.SaveDexLaneMeterAsync(1, BuildLaneDto());
        Assert.That(_sut.CapturedSql, Does.Contain("SaveDEXLaneMeter"));
    }

    [Test]
    public async Task SaveDexLaneMeterAsync_PassesDexMeterIdParameter()
    {
        await _sut.SaveDexLaneMeterAsync(42, BuildLaneDto());
        var param = GetParam("@DexMeterId");
        Assert.That(param.Value, Is.EqualTo(42));
    }

    [Test]
    public async Task SaveDexLaneMeterAsync_PassesProductIdentifierParameter()
    {
        await _sut.SaveDexLaneMeterAsync(1, BuildLaneDto(productId: "101"));
        var param = GetParam("@ProductIdentifier");
        Assert.That(param.Value, Is.EqualTo("101"));
    }

    [Test]
    public async Task SaveDexLaneMeterAsync_PassesPriceParameter()
    {
        await _sut.SaveDexLaneMeterAsync(1, BuildLaneDto(price: 3.25m));
        var param = GetParam("@Price");
        Assert.That(param.Value, Is.EqualTo(3.25m));
    }

    [Test]
    public async Task SaveDexLaneMeterAsync_PassesNumberOfVendsParameter()
    {
        await _sut.SaveDexLaneMeterAsync(1, BuildLaneDto(vends: 4));
        var param = GetParam("@NumberOfVends");
        Assert.That(param.Value, Is.EqualTo(4));
    }

    [Test]
    public async Task SaveDexLaneMeterAsync_PassesValueOfPaidSalesParameter()
    {
        await _sut.SaveDexLaneMeterAsync(1, BuildLaneDto(sales: 13.00m));
        var param = GetParam("@ValueOfPaidSales");
        Assert.That(param.Value, Is.EqualTo(13.00m));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SqlParameter GetParam(string name) =>
        _sut.CapturedParameters!.First(p => p.ParameterName == name);

    private static DexMeterDto BuildMeterDto(
        string machine = "A",
        DateTime dexDateTime = default,
        string serial = "100077238",
        decimal vends = 344.50m) =>
        new()
        {
            Machine = machine,
            DexDateTime = dexDateTime == default ? new DateTime(2023, 12, 10, 23, 10, 0) : dexDateTime,
            MachineSerialNumber = serial,
            ValueOfPaidVends = vends
        };

    private static DexLaneMeterDto BuildLaneDto(
        string productId = "101",
        decimal price = 3.25m,
        int vends = 4,
        decimal sales = 13.00m) =>
        new()
        {
            ProductIdentifier = productId,
            Price = price,
            NumberOfVends = vends,
            ValueOfPaidSales = sales
        };
}

// ── Test double ───────────────────────────────────────────────────────────────

internal sealed class CapturingDexRepository : DexRepository
{
    public string? CapturedSql { get; private set; }
    public SqlParameter[]? CapturedParameters { get; private set; }
    public int OutputValue { get; set; } = 1;

    public CapturingDexRepository() : base(CreateContext()) { }

    private static VenDexDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VenDexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VenDexDbContext(options);
    }

    protected override Task ExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        CapturedSql = sql;
        CapturedParameters = parameters;

        var outParam = parameters.FirstOrDefault(p => p.Direction == ParameterDirection.Output);
        if (outParam is not null) outParam.Value = OutputValue;

        return Task.CompletedTask;
    }
}
