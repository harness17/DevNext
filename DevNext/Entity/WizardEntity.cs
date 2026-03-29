using Dev.CommonLibrary.Entity;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// 多段階フォームサンプルエンティティ
    /// ウィザード完了時にすべてのステップのデータをまとめて保存する
    /// </summary>
    public class WizardEntity : SiteEntityBase
    {
        // ─── Step 1: 基本情報 ────────────────────────────────────

        /// <summary>氏名</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        /// <summary>メールアドレス</summary>
        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = "";

        /// <summary>電話番号（任意）</summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        // ─── Step 2: 詳細情報 ────────────────────────────────────

        /// <summary>件名</summary>
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = "";

        /// <summary>内容</summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = "";

        /// <summary>カテゴリ</summary>
        public WizardCategory Category { get; set; }

        /// <summary>希望対応日（任意）</summary>
        public DateTime? DesiredDate { get; set; }
    }
}
