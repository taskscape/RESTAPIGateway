using System.Data;
using System.Security.Claims;
using System.Security.Principal;

namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        private static bool HasTablePermission(IConfigurationSection tableSection, string permission, ClaimsPrincipal? User = null)
        {
            if (!tableSection.Exists())
                return false;

            List<string>? configPermissions = tableSection.Get<List<string>>();
            if (configPermissions.Contains(permission))
                return true;

            IConfigurationSection roleSection = tableSection.GetSection(permission);
            if (!roleSection.Exists()) return false;

            List<string>? rolePermissions = roleSection.Get<List<string>>();
            if (rolePermissions?.FirstOrDefault() == "*") return true;

            if (User != null)
            {
                string[]? roles = GetUserRoles(User);
                if (roles?.Length > 0)
                    return rolePermissions
                        .Select(x => x.StartsWith("rolename:") || x.StartsWith("username:") ? x : $"rolename:{x}")
                        .Intersect(roles, StringComparer.OrdinalIgnoreCase)
                        .Any();
            }

            return false;
        }

        private static bool HasProcedurePermission(IConfigurationSection tableSection, ClaimsPrincipal user)
        {
            if (!tableSection.Exists())
                return false;

            List<string>? rolePermissions = tableSection.Get<List<string>>();

            if (rolePermissions?.FirstOrDefault() == "*") return true;

            if (user != null)
            {
                string[]? roles = GetUserRoles(user);
                if (roles?.Length > 0)
                {
                    return rolePermissions != null && rolePermissions.Count != 0 && rolePermissions
                        .Select(x => x.StartsWith("rolename:") || x.StartsWith("username:") ? x : $"rolename:{x}")
                        .Intersect(roles, StringComparer.OrdinalIgnoreCase)
                        .Any();
                }

            }

            return false;
        }


        /// <summary>
        /// Checks is user has a permission to a database table
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="tableName"></param>
        /// <param name="permission"></param>
        /// <param name="User"></param>
        /// <returns></returns>

        public static bool CheckTablePermission(IConfiguration configuration, string tableName, string permission, ClaimsPrincipal? User = null)
        {
            if (!configuration.GetSection("Database").Exists()) return false;
            return HasTablePermission(configuration.GetSection("Database:Tables:*"), permission, User) ||
                HasTablePermission(configuration.GetSection("Database:Tables:" + tableName), permission, User);
        }

        /// <summary>
        /// Checks is user has a permission to a stored procedure
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="procedureName"></param>
        /// <param name="user"></param>
        /// <returns></returns>

        public static bool CheckProcedurePermission(IConfiguration configuration, string procedureName, ClaimsPrincipal user)
        {
            if (!configuration.GetSection("Database").Exists()) return false;
            _ = configuration.GetSection("Database:Procedures:" + procedureName);
            return HasProcedurePermission(configuration.GetSection("Database:Procedures:*"), user) ||
                HasProcedurePermission(configuration.GetSection("Database:Procedures:" + procedureName), user);
        }

        // Existing code...

        public static string[]? GetUserRoles(ClaimsPrincipal User)
        {
            List<string> userRoles;

            if (User.Identity is WindowsIdentity windowsIdentity && windowsIdentity.Groups != null)
            {
                // Windows user groups
                userRoles = [.. windowsIdentity.Groups
                    .Select(static x => x.Translate(typeof(NTAccount)).Value)
                    .Select(x => $"rolename:{x}")];

                string username = User.Identity.Name;
                if (username != null)
                    userRoles.Add($"username:{username}");
            }
            else
            {
                // Claims Roles
                userRoles = [.. User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => $"rolename:{c.Value}")];

                if (User.Identity?.Name != null)
                    userRoles.Add($"username:{User.Identity.Name}");
            }

            return [.. userRoles];
        }


    }
}
