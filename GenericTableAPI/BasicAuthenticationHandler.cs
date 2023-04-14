using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GenericTableAPI
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Authorization header not found.");

            try
            {
                var authenticationHeaderValue = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

                if (!"Basic".Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
                    return AuthenticateResult.NoResult();

                //Commented line throws an exception
                //var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationHeaderValue.Parameter)).Split(':', 2);
                var credentials = authenticationHeaderValue.Parameter.Split(':', 2);
                var username = credentials[0];
                var password = credentials[1];

                if (ValidateCredentials(username, password))
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, username) };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }

                return AuthenticateResult.Fail("Invalid username or password.");
            }
            catch (FormatException)
            {
                return AuthenticateResult.Fail("Invalid authorization header.");
            }
        }

        private bool ValidateCredentials(string username, string password)
        {
            return true;
        }
    }
}