namespace VendSys.Api.Auth;

/// <summary>Credentials loaded from the <c>BasicAuth</c> configuration section.</summary>
public sealed class BasicAuthOptions
{
    /// <summary>Expected username for HTTP Basic authentication.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Expected password for HTTP Basic authentication.</summary>
    public string Password { get; set; } = string.Empty;
}
