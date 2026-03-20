using Dev.CommonLibrary.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.Models;
using Site.Service;

namespace Site.Controllers
{
    /// <summary>
    /// メール送信サンプルコントローラー
    /// Email.cs + テキストテンプレートによるメール送信のサンプル
    /// </summary>
    [Authorize]
    [ServiceFilter(typeof(AccessLogAttribute))]
    public class MailSampleController : Controller
    {
        private readonly MailSampleService _mailService;

        public MailSampleController(MailSampleService mailService)
        {
            _mailService = mailService;
        }

        /// <summary>
        /// お問い合わせフォームを表示する
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View(new MailSampleViewModel());
        }

        /// <summary>
        /// フォーム送信を受け取り、確認メールを送信する
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(MailSampleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool success = _mailService.SendContactMail(model);
            if (!success)
            {
                // メール送信失敗時はエラーメッセージを表示してフォームを再表示
                ModelState.AddModelError("", "メール送信に失敗しました。SMTP設定を確認してください。");
                return View(model);
            }

            return RedirectToAction(nameof(Complete));
        }

        /// <summary>
        /// 送信完了ページを表示する
        /// </summary>
        public IActionResult Complete()
        {
            return View();
        }
    }
}
