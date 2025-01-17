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
        private readonly List<AuthUser>? _users = [];

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration config)
            : base(options, logger, encoder)
        {
            config.GetSection("BasicAuthSettings").Bind(_users);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header not found."));
            }

            try
            {
                AuthenticationHeaderValue authenticationHeaderValue =
                    AuthenticationHeaderValue.Parse(value);

                if (!"Basic".Equals(authenticationHeaderValue.Scheme, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(AuthenticateResult.NoResult());

                if (authenticationHeaderValue.Parameter == null)
                    return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header"));
                
                string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationHeaderValue.Parameter)).Split(':', 2);

                string? username = credentials.FirstOrDefault();
                string? password = credentials.LastOrDefault();

                if (password == null || username == null || !ValidateCredentials(username, password))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
                }

                if (_users == null) return Task.FromResult(AuthenticateResult.Fail("Invalid authorisation."));
                    
                string? role = _users.FirstOrDefault(x => x.Username == username)?.Role;
                List<Claim> claims = [new(ClaimTypes.Name, username)];
                if (!string.IsNullOrEmpty(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));

                ClaimsIdentity identity = new(claims, Scheme.Name);
                ClaimsPrincipal principal = new(identity);
                AuthenticationTicket ticket = new(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (FormatException exception)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header: " + exception.Message));
            }

        }

        private bool ValidateCredentials(string username, string password)
        {
            return _users != null && _users.Any(user => user.Username == username && user.Password == password);
        }
    }
}