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
        private readonly IConfiguration _configuration;
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration config)
            : base(options, logger, encoder, clock)
        {
            _configuration = config;
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

        private bool ValidateCredentials(string username, string password)
        {
            string? confUsername = _configuration.GetSection("BasicAuthSettings:Username").Value;
            string? confPassword = _configuration.GetSection("BasicAuthSettings:Password").Value;

            return confUsername == username && confPassword == password;
        }
    }
}