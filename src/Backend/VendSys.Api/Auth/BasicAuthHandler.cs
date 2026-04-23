using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VendSys.Api.Auth;

/// <summary>Validates HTTP Basic credentials against the <c>BasicAuth</c> configuration section.</summary>
public sealed class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<BasicAuthOptions> _credentials;

    public BasicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<BasicAuthOptions> credentials)
        : base(options, logger, encoder)
    {
        _credentials = credentials;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        byte[] credentialBytes;
        try
        {
            credentialBytes = Convert.FromBase64String(header.Parameter ?? string.Empty);
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        var decoded = Encoding.UTF8.GetString(credentialBytes);
        var colonIndex = decoded.IndexOf(':');
        if (colonIndex < 0)
            return Task.FromResult(AuthenticateResult.Fail("Invalid credential format."));

        var username = decoded[..colonIndex];
        var password = decoded[(colonIndex + 1)..];

        var creds = _credentials.CurrentValue;
        if (!string.Equals(username, creds.Username, StringComparison.Ordinal) ||
            !string.Equals(password, creds.Password, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));

        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"VendSys\"";
        return base.HandleChallengeAsync(properties);
    }
}
