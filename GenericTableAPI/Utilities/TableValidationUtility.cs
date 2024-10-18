namespace GenericTableAPI.Utilities
{
    public static class TableValidationUtility
    {
        public static bool ValidTablePermission(IConfiguration configuration, string tableName, string permission, string[]? Roles = null)
        {
            if (!configuration.GetSection("Database").Exists()) return true;
            IConfigurationSection tableSection = configuration.GetSection("Database:Tables:" + tableName);
            if (!tableSection.Exists()) return false;
            List<string>? configPermissions = tableSection.Get<List<string>>();
            if(configPermissions.Contains(permission))
                return true;

            IConfigurationSection roleSection = tableSection.GetSection(permission);
            if (!roleSection.Exists()) return false;

            List<string>? rolePermissions = roleSection.Get<List<string>>();
            if (rolePermissions?.FirstOrDefault() == "*") return true;

            if (Roles?.Length > 0)
                return rolePermissions.Intersect(Roles).Any();
            return false;
        }
    }
}
