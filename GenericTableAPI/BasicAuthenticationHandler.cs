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
            {
                return AuthenticateResult.Fail("Authorization header not found.");
            }

            try
            {
                AuthenticationHeaderValue authenticationHeaderValue =
                    AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

                if (!"Basic".Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
                    return AuthenticateResult.NoResult();

                string[]? credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationHeaderValue.Parameter)).Split(':', 2);

                string? username = credentials.FirstOrDefault();
                string? password = credentials.LastOrDefault();

                if (password == null || username == null || !ValidateCredentials(username, password))
                {
                    return AuthenticateResult.Fail("Invalid username or password.");
                }

                Claim[] claims = { new(ClaimTypes.Name, username) };
                ClaimsIdentity identity = new(claims, Scheme.Name);
                ClaimsPrincipal principal = new(identity);
                AuthenticationTicket ticket = new(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);

            }
            catch (FormatException)
            {
                return AuthenticateResult.Fail("Invalid authorization header.");
            }

        }

        private static bool ValidateCredentials(string username, string password)
        {
            // TODO: implement user validation logic
            return true;
        }
    }
}