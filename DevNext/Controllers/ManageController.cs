using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Entity;
using Site.Models;

namespace Site.Controllers
{
    /// <summary>
    /// ログイン中ユーザー自身のアカウント管理コントローラー（パスワード変更等）。
    /// </summary>
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Logger _logger = Logger.GetLogger();

        public ManageController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>アカウント管理トップ。</summary>
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // 認証済みなのに DB にユーザーが存在しない場合は DB 不整合の可能性がある
                _logger.Error(new LogModel($"認証済みユーザーが DB に見つかりません: UserId={_userManager.GetUserId(User)}"));
                return View("Error");
            }
            var model = new IndexViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user)
            };
            return View(model);
        }

        /// <summary>パスワード変更（POST）。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.Error(new LogModel($"認証済みユーザーが DB に見つかりません: UserId={_userManager.GetUserId(User)}"));
                return View("Error");
            }
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        /// <summary>パスワード変更フォーム表示。</summary>
        public IActionResult ChangePassword() => View();

        /// <summary>パスワード設定フォーム表示（外部ログイン等でパスワード未設定のユーザー向け）。</summary>
        public IActionResult SetPassword() => View();

        /// <summary>パスワード設定（POST）。パスワード未設定ユーザー向け。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.Error(new LogModel($"認証済みユーザーが DB に見つかりません: UserId={_userManager.GetUserId(User)}"));
                return View("Error");
            }
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
    }
}
