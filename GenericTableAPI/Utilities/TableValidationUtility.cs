using System.Security.Claims;
using System.Security.Principal;

namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        public static bool ValidTablePermission(IConfiguration configuration, string tableName, string permission, ClaimsPrincipal? User = null)
        {
            if (!configuration.GetSection("Database").Exists()) return false;
            IConfigurationSection tableSection = configuration.GetSection("Database:Tables:" + tableName);
            if (!tableSection.Exists())
            {
                tableSection = configuration.GetSection("Database:Tables:*");//Check default permission
                if (!tableSection.Exists())
                    return false;
            }
            List<string>? configPermissions = tableSection.Get<List<string>>();
            if(configPermissions.Contains(permission))
                return true;

            IConfigurationSection roleSection = tableSection.GetSection(permission);
            if (!roleSection.Exists()) return false;

            List<string>? rolePermissions = roleSection.Get<List<string>>();
            if (rolePermissions?.FirstOrDefault() == "*") return true;

            if (User != null)
            {
                var roles = GetUserRoles(User);
                if(roles?.Length > 0)
                    return rolePermissions
                        .Select(x => x.StartsWith("rolename:") || x.StartsWith("username:") ? x : $"rolename:{x}")
                        .Intersect(roles)
                        .Any();
            }

            return false;
        }

        public static string[]? GetUserRoles(ClaimsPrincipal User)
        {
            List<string> userClaims;

            if (User.Identity is WindowsIdentity windowsIdentity && windowsIdentity.Groups != null)
            {
                //Windows user groups
                userClaims = windowsIdentity.Groups
                .Select(x => x.Translate(typeof(NTAccount)).Value)
                //.Select(x => x.Substring(x.IndexOf('\\') + 1)) //Ignore "DOMAIN\"
                .Select(x => $"rolename:{x}")
                .ToList();

                string username = User.Identity.Name;
                if (username != null)
                    //userClaims.Add($"username:{username.Substring(username.IndexOf('\\') + 1)}");//Ignore "DOMAIN\"
                    userClaims.Add($"username:{username}");//Dont ignore "DOMAIN\"
            }
            else
            {
                //Claims Roles
                userClaims = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => $"rolename:{c.Value}")
                    .ToList();

                if (User.Identity?.Name != null)
                    userClaims.Add($"username:{User.Identity.Name}");
            }

            return userClaims.ToArray();
        }
    }
}
