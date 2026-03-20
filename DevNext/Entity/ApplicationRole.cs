using Microsoft.AspNetCore.Identity;

namespace Site.Entity
{
    /// <summary>
    /// アプリケーションロール
    /// </summary>
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
