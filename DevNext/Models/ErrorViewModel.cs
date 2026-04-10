namespace Site.Models
{
    /// <summary>
    /// エラーページ表示用ビューモデル
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>リクエストID（トレース用）</summary>
        public string? RequestId { get; set; }

        /// <summary>HTTPステータスコード（404, 500 など）</summary>
        public int StatusCode { get; set; }

        /// <summary>エラータイトル（画面に表示する見出し）</summary>
        public string ErrorTitle { get; set; } = "エラーが発生しました";

        /// <summary>エラーメッセージ（画面に表示する説明文）</summary>
        public string ErrorMessage { get; set; } = "しばらく時間をおいてから再度お試しください。";

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
