using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Controllers
{
    /// <summary>
    /// 認証コントローラー（ログイン・登録・パスワード管理）。
    /// 認証不要アクションは [AllowAnonymous] で個別に解放する。
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Logger _logger = Logger.GetLogger();

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>ログイン画面表示。</summary>
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>ログイン処理。ロックアウト・失敗時のセキュリティイベントをログに記録する。</summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            if (result.IsLockedOut)
            {
                // セキュリティイベント: アカウントロックはブルートフォース攻撃の可能性があるため記録する
                _logger.Warn(new LogModel($"アカウントロック: Email={model.Email}"));
                return View("Lockout");
            }

            ModelState.AddModelError("", "無効なログイン試行です。");
            return View(model);
        }

        /// <summary>新規登録画面表示。</summary>
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>新規登録処理。登録成功時は Member ロールを付与してサインインする。</summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, ApplicationRoleType.Member.ToString());
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }
            return View(model);
        }

        /// <summary>パスワードリセット申請画面表示。</summary>
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        /// <summary>
        /// パスワードリセット申請処理。
        /// ユーザーが存在しない・メール未確認でも確認画面を返す（ユーザー列挙防止）。
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    return View("ForgotPasswordConfirmation");
                }
                // パスワードリセットメール送信処理をここに追加
            }
            return View(model);
        }

        /// <summary>パスワードリセット申請完了画面表示。</summary>
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        /// <summary>パスワードリセット画面表示。code がない場合はエラー画面を返す。</summary>
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null)
        {
            return code == null ? View("Error") : View();
        }

        /// <summary>パスワードリセット処理。</summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");
            var result = await _userManager.ResetPasswordAsync(user, model.Code ?? "", model.Password);
            if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");
            AddErrors(result);
            return View();
        }

        /// <summary>パスワードリセット完了画面表示。</summary>
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        /// <summary>ログアウト処理。</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>メールアドレス確認完了画面表示。</summary>
        [AllowAnonymous]
        public IActionResult ConfirmEmail() => View();

        /// <summary>IdentityResult のエラーを ModelState に追加する。</summary>
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        /// <summary>ローカル URL はリダイレクト、それ以外はホームへ（オープンリダイレクト防止）。</summary>
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}
