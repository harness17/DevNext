using Microsoft.AspNetCore.Identity;
using Site.Common;
using Site.Entity;

namespace Site.Service
{
    public class CommonService
    {
        private readonly DBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommonService(DBContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
    }
}
