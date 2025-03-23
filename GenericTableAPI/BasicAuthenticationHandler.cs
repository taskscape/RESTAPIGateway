using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using GenericTableAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace GenericTableAPI
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly List<AuthUser>? _users = [];
        private readonly IMemoryCache _cache;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration config,
            IMemoryCache cache)
            : base(options, logger, encoder)
        {
            config.GetSection("BasicAuthSettings").Bind(_users);
            _cache = cache;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authHeader = Request.Headers["Authorization"].ToString();

            if (_cache.TryGetValue(authHeader, out AuthenticateResult cachedResult))
            {
                return cachedResult;
            }

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

                List<string>? roles = _users.FirstOrDefault(x => x.Username == username)?.GetRoles();
                List<Claim> claims = [new(ClaimTypes.Name, username)];
                foreach (var role in roles)
                    if (!string.IsNullOrEmpty(role))
                        claims.Add(new Claim(ClaimTypes.Role, role));

                ClaimsIdentity identity = new(claims, Scheme.Name);
                ClaimsPrincipal principal = new(identity);
                AuthenticationTicket ticket = new(principal, Scheme.Name);

                var result = AuthenticateResult.Success(ticket);

                _cache.Set(authHeader, result, TimeSpan.FromMinutes(5));

                return result;
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