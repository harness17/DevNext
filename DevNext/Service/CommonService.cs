using Microsoft.AspNetCore.Identity;
using Site.Common;
using Dev.CommonLibrary.Entity;
using Site.Entity;

namespace Site.Service
{
    /// <summary>
    /// 汎用サービス基底。将来的な共通処理の受け皿として用意している（現在は未実装）。
    /// </summary>
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
