using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using GenericTableAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GenericTableAPI
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly List<AuthUser>? _users = [];

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration config)
            : base(options, logger, encoder, clock)
        {
            _configuration = config;
            _configuration.GetSection("BasicAuthSettings").Bind(_users);
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

                string role = _users.Where(x => x.Username == username).FirstOrDefault().Role;
                List<Claim> claims = [new Claim(ClaimTypes.Name, username)];
                if (!string.IsNullOrEmpty(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));

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
            return _users.Any(user => user.Username == username && user.Password == password);
        }
    }
}