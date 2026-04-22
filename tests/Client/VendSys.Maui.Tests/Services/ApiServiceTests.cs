using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using VendSys.Maui;
using VendSys.Maui.Services;

namespace VendSys.Maui.Tests.Services;

[TestFixture]
public class ApiServiceTests
{
    // ── Builders ──────────────────────────────────────────────────────────────

    private static (ApiService sut, CapturingHandler handler) Build(params HttpStatusCode?[] codes)
    {
        var handler = new CapturingHandler(codes);
        var sut = CreateSut(services =>
            services
                .AddHttpClient(ApiConstants.HttpClientName,
                    c => c.BaseAddress = new Uri("http://test-host"))
                .ConfigurePrimaryHttpMessageHandler(() => handler));
        return (sut, handler);
    }

    private static (ApiService sut, CapturingHandler handler) BuildWithPolly(params HttpStatusCode?[] codes)
    {
        var handler = new CapturingHandler(codes);
        var sut = CreateSut(services =>
            services
                .AddHttpClient(ApiConstants.HttpClientName,
                    c => c.BaseAddress = new Uri("http://test-host"))
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(1))));
        return (sut, handler);
    }

    private static ApiService CreateSut(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return new ApiService(services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>());
    }

    // ── Request shape ─────────────────────────────────────────────────────────

    [Test]
    public async Task SendDexFileAsync_PostsToCorrectEndpoint()
    {
        var (sut, handler) = Build(HttpStatusCode.OK);
        await sut.SendDexFileAsync("A", "content");
        Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/vdi-dex?machine=A"));
    }

    [Test]
    public async Task SendDexFileAsync_SetsCorrectBasicAuthHeader()
    {
        var (sut, handler) = Build(HttpStatusCode.OK);
        await sut.SendDexFileAsync("A", "content");
        var auth = handler.Requests[0].Headers.Authorization!;
        Assert.That(auth.Scheme, Is.EqualTo("Basic"));
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter!));
        Assert.That(decoded, Is.EqualTo("vendsys:NFsZGmHAGWJSZ#RuvdiV"));
    }

    [Test]
    public async Task SendDexFileAsync_SetsTextPlainContentType()
    {
        var (sut, handler) = Build(HttpStatusCode.OK);
        await sut.SendDexFileAsync("A", "content");
        var ct = handler.Requests[0].Content!.Headers.ContentType!;
        Assert.That(ct.MediaType, Is.EqualTo("text/plain"));
        Assert.That(ct.CharSet, Is.EqualTo("utf-8"));
    }

    // ── Response mapping ──────────────────────────────────────────────────────

    [Test]
    public async Task SendDexFileAsync_Returns_Success_On200()
    {
        var (sut, _) = Build(HttpStatusCode.OK);
        var result = await sut.SendDexFileAsync("A", "content");
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task SendDexFileAsync_Returns_Failure_On500()
    {
        var (sut, _) = Build(HttpStatusCode.InternalServerError);
        var result = await sut.SendDexFileAsync("A", "content");
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("500"));
        });
    }

    [Test]
    public async Task SendDexFileAsync_Returns_Failure_OnHttpRequestException()
    {
        var (sut, _) = Build(new HttpStatusCode?[] { null }); // null entry triggers HttpRequestException
        var result = await sut.SendDexFileAsync("A", "content");
        Assert.That(result.IsSuccess, Is.False);
    }

    // ── Polly ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task SendDexFileAsync_RetriesThreeTimes_On503()
    {
        // 1 original attempt + 3 retries = 4 total calls
        var (sut, handler) = BuildWithPolly(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable);

        await sut.SendDexFileAsync("A", "content");

        Assert.That(handler.Requests, Has.Count.EqualTo(4));
    }
}

// ── Test double ───────────────────────────────────────────────────────────────

internal sealed class CapturingHandler : HttpMessageHandler
{
    private readonly Queue<HttpStatusCode?> _codes;
    public List<HttpRequestMessage> Requests { get; } = [];

    public CapturingHandler(params HttpStatusCode?[] codes) =>
        _codes = new Queue<HttpStatusCode?>(codes);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        var code = _codes.Dequeue();
        if (code is null) throw new HttpRequestException("Simulated network failure.");
        return Task.FromResult(new HttpResponseMessage(code.Value));
    }
}
