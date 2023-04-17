using Microsoft.Extensions.Configuration;

namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        public static bool ValidTablePermission(IConfiguration configuration, string tableName, string permission)
        {
            if (configuration.GetSection("Database").Exists())
            {
                List<string> configPermissions = configuration.GetSection("Database:Tables:" + tableName).Get<List<string>>();
                if (configPermissions == null || !configPermissions.Contains(permission))
                    return false;
            }
            return true;
        }
    }
}
