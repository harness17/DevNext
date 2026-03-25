using Site.Entity;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// メール送信ログエンティティ
    /// お問い合わせフォームからのメール送信結果を記録する。
    /// ログデータのため論理削除・履歴テーブルは持たない。
    /// </summary>
    public class MailLogEntity : SiteEntityBase
    {
        /// <summary>送信者名（フォーム入力値）</summary>
        [Required]
        [MaxLength(100)]
        public string SenderName { get; set; } = "";

        /// <summary>送信者メールアドレス（フォーム入力値）</summary>
        [Required]
        [MaxLength(256)]
        public string SenderEmail { get; set; } = "";

        /// <summary>件名（フォーム入力値）</summary>
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = "";

        /// <summary>本文（フォーム入力値）</summary>
        [Required]
        [MaxLength(2000)]
        public string Body { get; set; } = "";

        /// <summary>送信成功フラグ（true: 成功、false: 失敗）</summary>
        public bool IsSuccess { get; set; }

        /// <summary>エラーメッセージ（送信失敗時のみセット）</summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
