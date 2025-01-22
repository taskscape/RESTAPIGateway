using System.Data;

namespace GenericTableAPI.Models
{
    public class AuthUser
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? Role { get; set; }
        public List<string> Roles { get; set; } = [];

        public List<string> GetRoles()
        {
            List<string> combinedRoles = new(Roles);
            if(!string.IsNullOrWhiteSpace(Role))
                combinedRoles.Add(Role);
            return combinedRoles;
        }
    }
}
