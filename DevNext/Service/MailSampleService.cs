using Site.Common;
using Site.Models;

namespace Site.Service
{
    /// <summary>
    /// メール送信サンプルサービス
    /// </summary>
    public class MailSampleService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public MailSampleService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        /// <summary>
        /// お問い合わせメールを送信する。
        /// wwwroot/Templates/Mail/Contact.txt をテンプレートとして使用し、
        /// プレースホルダ ({Name} 等) をフォーム入力値で置換して送信する。
        /// </summary>
        /// <param name="model">フォーム入力内容</param>
        /// <returns>送信成功の場合 true</returns>
        public bool SendContactMail(MailSampleViewModel model)
        {
            // テンプレートファイルを読み込む（存在しない場合はデフォルト文面を使用）
            var templatePath = Path.Combine(_env.WebRootPath, "Templates", "Mail", "Contact.txt");
            string body = File.Exists(templatePath)
                ? File.ReadAllText(templatePath)
                : "{Name} 様\r\n\r\nお問い合わせを受け付けました。\r\n\r\n件名：{Subject}\r\n内容：{Body}";

            // プレースホルダを実際の値で置換
            body = body
                .Replace("{Name}", model.Name)
                .Replace("{Email}", model.Email)
                .Replace("{Subject}", model.Subject)
                .Replace("{Body}", model.Body)
                .Replace("{Date}", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));

            var fromAddress = _config["Mail:From"] ?? "noreply@example.com";

            var mail = new Email(_config)
            {
                From = fromAddress,
                FromName = _config["Mail:FromName"] ?? "DevNext",
                Subject = $"【お問い合わせ受付】{model.Subject}",
                Body = body
            };

            // 問い合わせ者本人に確認メールを送信
            mail.ToList.Add(model.Email);

            // 管理者宛 BCC（appsettings に設定されている場合）
            var adminAddress = _config["Mail:AdminAddress"];
            if (!string.IsNullOrEmpty(adminAddress))
                mail.BccList.Add(adminAddress);

            return mail.SendMail() == Email.SendMailResult.Success;
        }
    }
}
