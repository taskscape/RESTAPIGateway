using Microsoft.Extensions.Configuration;

namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        public static bool ValidTablePermission(IConfiguration configuration, string tableName, string permission)
        {
            if (configuration.GetSection("Database").Exists())
            {
                var tableSection = configuration.GetSection("Database:Tables:" + tableName);
                if (tableSection.Exists())
                {
                    List<string> configPermissions = tableSection.Get<List<string>>();
                    if (!configPermissions.Contains(permission))
                        return false;
                }
            }
            return true;
        }
    }
}
