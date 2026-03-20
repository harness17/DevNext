using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Common;
using Site.Entity;
using System.ComponentModel.DataAnnotations;

namespace Site.Models
{
    public class DatabaseSampleViewModel : SearchModelBase
    {
        public DatabaseSampleDataViewModel RowData { get; set; } = new();
        public DatabaseSampleCondViewModel Cond { get; set; } = new();
        public IEnumerable<SelectListItem> RecoedNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    public class DatabaseSampleCondViewModel : SearchCondModelBase
    {
        public DatabaseSampleCondViewModel()
        {
            BoolDataSelectList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "全て" },
                new SelectListItem { Value = "true", Text = "あり" },
                new SelectListItem { Value = "false", Text = "無し" }
            };
        }

        [MaxLength(128)]
        public string? ApplicationUserId { get; set; }
        public List<SelectListItem> ApplicationUserIdist { get; set; } = new();
        public string? StringData { get; set; }
        public int? IntData { get; set; }
        public bool? BoolData { get; set; }
        public List<SelectListItem> BoolDataSelectList { get; set; }
        public List<string> EnumData { get; set; } = new();
        public List<SelectListItem> EnumDataList { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum>().ToList();
        public SampleEnum2? EnumData2 { get; set; }
        public List<SelectListItem> EnumData2List { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum2>().ToList();

        [Display(Name = "作成日(開始)")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Create_start_date { get; set; }

        [Display(Name = "作成日(終了)")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Create_end_date { get; set; }
    }

    public class DatabaseSampleDataViewModel
    {
        public List<SampleEntity> rows { get; set; } = new();
        public CommonListSummaryModel? Summary { get; set; }
    }

    public class DatabaseSampleDetailViewModel
    {
        public long? Id { get; set; }

        [Required]
        public string StringData { get; set; } = "";
        public int IntData { get; set; }
        public bool BoolData { get; set; }
        public SampleEnum? EnumData { get; set; }
        public List<SelectListItem> EnumDataList { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum>().ToList();
        public SampleEnum2 EnumData2 { get; set; }
        public List<SelectListItem> EnumData2List { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum2>().ToList();

        [FileSize(MaxSize: 1024 * 1024 * 2)]
        [FileTypes(types: "pdf,xls,xlsx,doc,docx,ppt,pptx,png,jpeg,jpg,gif")]
        public IFormFile[]? FileData_file { get; set; }

        public string? FileData { get; set; }
    }

    public class DatabaseSampleImportViewModel
    {
        [Required]
        [FileSize(MaxSize: 1024 * 1024 * 2)]
        [FileTypes(types: "xlsx")]
        public IFormFile? ImportDataFile { get; set; }

        public List<string> ImportErrList { get; set; } = new();
    }

    public class DatabaseMapperUsageSampleViewModel
    {
        public List<DatabaseMapperUsageSampleDataModel> Data { get; set; } = new();
    }

    public class DatabaseMapperUsageSampleDataModel
    {
        public string? ApplicationUserId { get; set; }
        public string? MapperUsageStringData { get; set; }
        public int MapperUsageIntData { get; set; }
        public bool MapperUsageBoolData { get; set; }
        public SampleEnum MapperUsageEnumData { get; set; }
        public string? StringDataWithMapper { get; set; }
        public int IntDataWithMapper { get; set; }
        public bool BoolDataWithMapper { get; set; }
        public SampleEnum EnumDataWithMapper { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
