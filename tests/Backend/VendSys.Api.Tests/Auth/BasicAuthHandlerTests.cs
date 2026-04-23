using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace VendSys.Api.Tests.Auth;

[TestFixture]
public class BasicAuthHandlerTests
{
    private TestWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    // Minimal DEX that satisfies the parser (real IDexParserService runs in the test host)
    private const string MinimalDexA =
        "DXS*STF0000000*VA*V0/6*1\n" +
        "ST*001*0001\n" +
        "ID1*100077238*187**Location Not Set**MerchantG*6*1\n" +
        "ID5*20231210*2310*53*\n" +
        "VA1*34450*195*600*4*0*0*0*0*0*0*0*0\n" +
        "PA1*101*325*101****0**\n" +
        "PA2*4*1300*0*0*0*0*0*0*0*0*0*0\n" +
        "DXE*1*1\n";

    [SetUp]
    public void SetUp()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static string BasicHeader(string credentials) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

    private HttpRequestMessage BuildPost(string? authHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/vdi-dex?machine=A")
        {
            Content = new StringContent(MinimalDexA, Encoding.UTF8, "text/plain")
        };
        if (authHeader is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        return request;
    }

    [Test]
    public async Task ValidCredentials_Returns200()
    {
        var response = await _client.SendAsync(BuildPost(BasicHeader("testuser:testpass")));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task WrongPassword_Returns401()
    {
        var response = await _client.SendAsync(BuildPost(BasicHeader("testuser:wrong")));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }

    [Test]
    public async Task WrongUsername_Returns401()
    {
        var response = await _client.SendAsync(BuildPost(BasicHeader("wrong:testpass")));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }

    [Test]
    public async Task MissingAuthHeader_Returns401()
    {
        var response = await _client.SendAsync(BuildPost(authHeader: null));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }

    [Test]
    public async Task MalformedBase64_Returns401()
    {
        var response = await _client.SendAsync(BuildPost("not-valid-base64!!!"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }

    [Test]
    public async Task NonBasicScheme_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/vdi-dex?machine=A")
        {
            Content = new StringContent(MinimalDexA, Encoding.UTF8, "text/plain")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sometoken");

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }

    [Test]
    public async Task CredentialsMissingColon_Returns401()
    {
        // "testuser" base64 encoded — no colon in decoded value
        var response = await _client.SendAsync(BuildPost(BasicHeader("testuser")));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.Contain("Basic realm=\"VendSys\""));
    }
}
