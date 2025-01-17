using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GenericTableAPI;

public class NoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        List<Claim>? claims = new List<Claim>();
        ClaimsIdentity? identity = new ClaimsIdentity(claims, Scheme.Name);
        ClaimsPrincipal? principal = new ClaimsPrincipal(identity);
        AuthenticationTicket? ticket = new AuthenticationTicket(principal, Scheme.Name);

        AuthenticateResult? result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
