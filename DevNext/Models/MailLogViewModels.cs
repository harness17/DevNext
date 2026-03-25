using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Entity;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>
    /// メール送信ログ一覧 ViewModel
    /// </summary>
    public class MailLogViewModel : SearchModelBase
    {
        public MailLogListDataViewModel RowData { get; set; } = new();
        public MailLogCondSearchViewModel Cond { get; set; } = new();

        /// <summary>表示件数ドロップダウン用リスト</summary>
        public IEnumerable<SelectListItem> RecoedNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    /// <summary>
    /// メール送信ログ一覧 検索条件 ViewModel（View ↔ Controller 間）
    /// </summary>
    public class MailLogCondSearchViewModel : SearchCondModelBase
    {
        public MailLogCondSearchViewModel()
        {
            // 成功/失敗フィルタ用ラジオボタンリスト
            IsSuccessList = new List<SelectListItem>
            {
                new SelectListItem { Value = "",      Text = "全て" },
                new SelectListItem { Value = "true",  Text = "成功" },
                new SelectListItem { Value = "false", Text = "失敗" }
            };
        }

        [Display(Name = "送信者名")]
        [MaxLength(100)]
        public string? SenderName { get; set; }

        [Display(Name = "送信者メール")]
        [MaxLength(256)]
        public string? SenderEmail { get; set; }

        [Display(Name = "送信結果")]
        public bool? IsSuccess { get; set; }

        /// <summary>成功/失敗ラジオボタン用リスト</summary>
        public List<SelectListItem> IsSuccessList { get; set; }

        [Display(Name = "送信日付(開始)")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? SentFrom { get; set; }

        [Display(Name = "送信日付(終了)")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? SentTo { get; set; }
    }

    /// <summary>
    /// メール送信ログ一覧データ ViewModel（リポジトリからの取得結果）
    /// </summary>
    public class MailLogListDataViewModel
    {
        public List<MailLogEntity> Rows { get; set; } = new();
        public Dev.CommonLibrary.Common.CommonListSummaryModel? Summary { get; set; }
    }
}
