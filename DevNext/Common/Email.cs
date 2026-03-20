using System.Net.Mail;

namespace Site.Common
{
    /// <summary>
    /// メール送信クラス
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
                var smtpHost = _config["Smtp:Host"] ?? "localhost";
                var smtpPort = int.Parse(_config["Smtp:Port"] ?? "25");

                using var mailer = new SmtpClient(smtpHost, smtpPort);
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
