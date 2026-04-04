using ExcelSample.Entity;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ExcelSample.Models
{
    // ─────────────────────────────────────────────
    // アカウント
    // ─────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "電子メール")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = "";

        [Display(Name = "このアカウントを記憶する")]
        public bool RememberMe { get; set; }
    }

    // ─────────────────────────────────────────────
    // 一覧画面
    // ─────────────────────────────────────────────

    /// <summary>
    /// ExcelSample 一覧画面 ViewModel
    /// ポイント: ExcelSample は全件表示のためページングなし
    /// </summary>
    public class ExcelSampleViewModel
    {
        /// <summary>一覧データ</summary>
        public List<ExcelItemEntity> Rows { get; set; } = new();

        /// <summary>インポート用ファイル</summary>
        public IFormFile? ImportFile { get; set; }

        /// <summary>インポートエラーリスト（行番号 + エラーメッセージ）</summary>
        public List<string> ImportErrors { get; set; } = new();

        /// <summary>インポート成功件数</summary>
        public int ImportSuccessCount { get; set; }
    }
}
