using System.Net;
using System.Net.Mail;

namespace MailSample.Common
{
    /// <summary>
    /// メール送信クラス。
    /// SMTP設定は appsettings.json の Smtp セクションから読み込む。
    /// </summary>
    public class Email
    {
        public string? From { get; set; }
        public string? FromName { get; set; }
        public string? ReplyTo { get; set; }
        public List<string> ToList { get; set; } = new List<string>();
        public List<string> CcList { get; set; } = new List<string>();
        public List<string> BccList { get; set; } = new List<string>();
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public List<string> attachList { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
        public string? SendmailScene { get; set; }
        public int? GroupMailId { get; set; }
        public int? GroupMailTargetId { get; set; }
        public string? f_done_note { get; set; }

        private readonly IConfiguration _config;

        public enum SendMailResult { Success, Failure }

        public Email(IConfiguration config)
        {
            _config = config;
        }

        public SendMailResult SendMail()
        {
            try
            {
                // --- SMTP 接続設定 ---
                // appsettings.json の Smtp セクションから各設定を取得する
                // 設定が存在しない場合のデフォルト値:
                //   Host: localhost、Port: 25、EnableSsl: false、Timeout: 30秒
                var smtpHost = _config["Smtp:Host"] ?? "localhost";
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "25");
                var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "false");
                var userName = _config["Smtp:UserName"];
                var password = _config["Smtp:Password"];
                var timeout = int.Parse(_config["Smtp:Timeout"] ?? "30000");

                using var mailer = new SmtpClient(smtpHost, smtpPort);

                // ポイント: SSL/TLS の有効化
                // Port 465 (SMTPS) または 587 (STARTTLS) を使う場合は true にする
                // Port 25 (平文) では false のまま使用する
                mailer.EnableSsl = enableSsl;

                // ポイント: SMTP 認証（ユーザー名・パスワードが設定されている場合のみ有効）
                // Gmail / SendGrid / Mailgun 等の外部SMTPサービスでは認証が必須
                // 社内 SMTP サーバーや MailHog（開発用）では認証不要の場合が多い
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                    mailer.Credentials = new NetworkCredential(userName, password);

                // ポイント: 送信タイムアウト（ミリ秒）
                // デフォルト 100000ms（100秒）は長すぎるためアプリの要件に合わせて設定する
                mailer.Timeout = timeout;

                using var message = CreateMessage();
                mailer.Send(message);
                return SendMailResult.Success;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                f_done_note = ErrorMessage;
                return SendMailResult.Failure;
            }
        }

        private MailMessage CreateMessage()
        {
            var message = new MailMessage();
            var enc = System.Text.Encoding.UTF8;
            message.BodyEncoding = enc;

            if (!string.IsNullOrEmpty(From))
                message.From = new MailAddress(From, FromName);

            foreach (var to in ToList)
                message.To.Add(new MailAddress(to));

            if (!string.IsNullOrEmpty(ReplyTo))
                message.ReplyToList.Add(new MailAddress(ReplyTo));

            foreach (var cc in CcList)
                message.CC.Add(new MailAddress(cc));

            foreach (var bcc in BccList)
                message.Bcc.Add(new MailAddress(bcc));

            // ポイント: 件名を Base64 エンコード（B エンコード）して日本語を扱えるようにする
            message.Subject = EncodeMailHeader(Subject ?? "", enc);
            message.Body = Body;

            foreach (var attach in attachList)
            {
                try
                {
                    var a = new Attachment(attach);
                    a.Name = Path.GetFileName(attach);
                    message.Attachments.Add(a);
                }
                catch { }
            }

            return message;
        }

        private string EncodeMailHeader(string str, System.Text.Encoding enc)
        {
            string ret = Convert.ToBase64String(enc.GetBytes(str));
            return $"=?{enc.BodyName}?B?{ret}?=";
        }
    }
}
