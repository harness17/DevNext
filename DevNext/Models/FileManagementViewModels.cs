using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Entity;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    /// <summary>
    /// ファイル管理一覧ViewModel（検索条件・一覧データを保持）
    /// </summary>
    public class FileManagementViewModel : SearchModelBase
    {
        public FileManagementDataViewModel RowData { get; set; } = new();
        public FileManagementCondViewModel Cond { get; set; } = new();
        public IEnumerable<SelectListItem> RecoedNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    /// <summary>
    /// 検索条件ViewModel
    /// </summary>
    public class FileManagementCondViewModel : SearchCondModelBase
    {
        [Display(Name = "ファイル名")]
        public string? OriginalFileName { get; set; }

        [Display(Name = "説明")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 一覧データViewModel
    /// </summary>
    public class FileManagementDataViewModel
    {
        public List<FileEntity> rows { get; set; } = new();
        public CommonListSummaryModel? Summary { get; set; }
    }

    /// <summary>
    /// ファイルアップロードViewModel
    /// </summary>
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "ファイルを選択してください")]
        [Display(Name = "ファイル")]
        public IFormFile? UploadFile { get; set; }

        [MaxLength(500)]
        [Display(Name = "説明")]
        public string? Description { get; set; }
    }
}
