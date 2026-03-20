using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    /// <summary>
    /// ファイル管理エンティティ（ファイルのメタ情報を保持する）
    /// 物理ファイルは wwwroot/Uploads/Files/ 配下に SavedFileName で保存する
    /// </summary>
    public class FileEntity : SiteEntityBase
    {
        /// <summary>アップロード時の元ファイル名（表示・ダウンロード時に使用）</summary>
        [Required]
        [MaxLength(260)]
        public string OriginalFileName { get; set; } = "";

        /// <summary>
        /// サーバー上の保存ファイル名（GUID + 拡張子）
        /// ファイル名の重複・パストラバーサルを防ぐため、GUID で管理する
        /// </summary>
        [Required]
        [MaxLength(260)]
        public string SavedFileName { get; set; } = "";

        /// <summary>ファイルサイズ（バイト）</summary>
        public long FileSize { get; set; }

        /// <summary>MIMEタイプ（Content-Type）</summary>
        [MaxLength(100)]
        public string ContentType { get; set; } = "";

        /// <summary>ファイルの説明（任意）</summary>
        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
