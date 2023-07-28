namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        public static bool ValidTablePermission(IConfiguration configuration, string tableName, string permission)
        {
            if (!configuration.GetSection("Database").Exists()) return true;
            IConfigurationSection tableSection = configuration.GetSection("Database:Tables:" + tableName);
            if (!tableSection.Exists()) return false;
            List<string> configPermissions = tableSection.Get<List<string>>();
            return configPermissions.Contains(permission);
        }
    }
}
