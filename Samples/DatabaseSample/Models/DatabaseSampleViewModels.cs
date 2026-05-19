using DatabaseSample.Common;
using DatabaseSample.Entity;
using Dev.CommonLibrary.Attributes;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DatabaseSample.Models
{
    // ポイント: SearchModelBase を継承することでページング・ソートに必要な
    //           Page / Sort / SortDir / RecordNum / PageRead プロパティを持つ
    //           一覧画面 ViewModel はこのクラスを継承するのがパターン
    public class DatabaseSampleViewModel : SearchModelBase
    {
        public DatabaseSampleDataViewModel RowData { get; set; } = new();
        public DatabaseSampleCondViewModel Cond { get; set; } = new();
        // ポイント: 表示件数ドロップダウン用のリストを共通ユーティリティで生成（10/25/50/100 件等）
        public IEnumerable<SelectListItem> RecoedNumberList { get; } = LocalUtil.SetRecoedNumberList();
    }

    // ポイント: SearchCondModelBase を継承して検索条件 + Pager 情報を一つのオブジェクトにまとめる
    //           TempData に JSON シリアライズして保存することでページング時に条件を維持する
    public class DatabaseSampleCondViewModel : SearchCondModelBase
    {
        public DatabaseSampleCondViewModel()
        {
            // ポイント: bool? に対応したラジオボタン用リストをコンストラクタで初期化
            //           "全て"（Value=""）を先頭に置くことで未選択状態を表現する
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
        // ポイント: チェックボックス複数選択は List<string> で受け取る
        //           View 側では name="Cond.EnumData" を複数チェックボックスに指定することでリスト binding される
        public List<string> EnumData { get; set; } = new();
        // ポイント: SelectListUtility.GetEnumSelectListItem<T>() で Enum の定義からドロップダウン用リストを自動生成
        //           Enum の Display 属性が設定されていれば表示名として使われる
        public List<SelectListItem> EnumDataList { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum>().ToList();
        public SampleEnum2? EnumData2 { get; set; }
        public List<SelectListItem> EnumData2List { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum2>().ToList();

        // ポイント: DisplayFormat で date input に渡す書式を指定する
        //           ApplyFormatInEditMode=true で編集フォームにも書式を適用する
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

    // ポイント: 新規作成・編集で共用する詳細 ViewModel
    //           Id が null → 新規作成、値あり → 編集 として Controller/View で判定する
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

        // ポイント: カスタムバリデーション属性（CommonLibrary で定義）
        //           [FileSize] → ファイルサイズ上限チェック（バイト単位）
        //           [FileTypes] → 許可する拡張子をカンマ区切りで指定
        [FileSize(MaxSize: 1024 * 1024 * 2)]
        [FileTypes(types: "pdf,xls,xlsx,doc,docx,ppt,pptx,png,jpeg,jpg,gif")]
        // ポイント: IFormFile[] で複数ファイルアップロードに対応
        //           View 側の input に multiple 属性が必要。enctype="multipart/form-data" も必須
        public IFormFile[]? FileData_file { get; set; }

        // ポイント: 保存済みファイル名をカンマ区切りで保持する文字列
        //           FileData_file とプレフィックスを合わせることで AutoMapper で元 Entity の FileData と対応させる
        public string? FileData { get; set; }
    }

    public class DatabaseSampleImportViewModel
    {
        [Required]
        [FileSize(MaxSize: 1024 * 1024 * 2)]
        [FileTypes(types: "xlsx")]
        public IFormFile? ImportDataFile { get; set; }

        // ポイント: サービス側でエラーメッセージを書き込む。空なら成功、1件以上なら画面にエラー表示する
        public List<string> ImportErrList { get; set; } = new();
    }

    // ポイント: AutoMapper の前付け名・後付け名マッピングのデモ用 ViewModel
    //           Service で RecognizeDestinationPrefixes / RecognizeDestinationPostfixes を設定して使用する
    public class DatabaseMapperUsageSampleViewModel
    {
        public List<DatabaseMapperUsageSampleDataModel> Data { get; set; } = new();
    }

    // ─────────────────────────────────────────────
    // 一括登録・編集用 ViewModel
    // ─────────────────────────────────────────────

    // ポイント: 親エンティティと任意件数の子エンティティを1フォームでまとめて登録・編集するための ViewModel
    //           Id が null → 新規登録、値あり → 編集 として Controller/View で判定する
    public class DatabaseSampleBulkEditViewModel
    {
        public long? Id { get; set; }

        [Display(Name = "文字列データ")]
        [Required(ErrorMessage = "文字列データは必須です。")]
        [MaxLength(200)]
        public string StringData { get; set; } = "";

        [Display(Name = "数値データ")]
        public int IntData { get; set; }

        [Display(Name = "BoolData")]
        public bool BoolData { get; set; }

        [Display(Name = "EnumData")]
        public SampleEnum? EnumData { get; set; }
        public List<SelectListItem> EnumDataList { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum>().ToList();

        [Display(Name = "EnumData2")]
        public SampleEnum2 EnumData2 { get; set; }
        public List<SelectListItem> EnumData2List { get; set; } = SelectListUtility.GetEnumSelectListItem<SampleEnum2>().ToList();

        // ポイント: 子エンティティの一覧。新規行は Id = null、既存行は Id に DB の主キーをセットする
        public List<DatabaseSampleBulkChildViewModel> Children { get; set; } = new();
    }

    // ポイント: 一括編集フォーム内の子エンティティ1行分のデータ
    //           Id が null → POST 時に新規 INSERT、値あり → UPDATE 対象
    public class DatabaseSampleBulkChildViewModel
    {
        // ポイント: 既存レコードの更新と新規追加を同一リストで扱うため Id は nullable にする
        public long? Id { get; set; }

        [Display(Name = "文字列データ")]
        [Required(ErrorMessage = "文字列データは必須です。")]
        [MaxLength(200)]
        public string StringData { get; set; } = "";

        [Display(Name = "数値データ")]
        public int IntData { get; set; }

        [Display(Name = "BoolData")]
        public bool BoolData { get; set; }
    }

    // ─────────────────────────────────────────────
    // 詳細画面用 ViewModel（親エンティティ + 子一覧）
    // ─────────────────────────────────────────────

    // ポイント: DBサンプルの詳細ページで親エンティティと紐づく子エンティティ一覧を同時に扱うための ViewModel
    public class DatabaseSampleDetailsViewModel
    {
        /// <summary>親エンティティ</summary>
        public SampleEntity Parent { get; set; } = new();

        /// <summary>子エンティティ一覧（論理削除済み除く）</summary>
        public List<SampleEntityChild> Children { get; set; } = new();
    }

    /// <summary>
    /// 一覧 PDF 印刷用 ViewModel。
    /// </summary>
    public class DatabaseSamplePdfListViewModel
    {
        public DateTime OutputDate { get; set; } = DateTime.Now;
        public List<SampleEntity> Rows { get; set; } = new();
    }

    /// <summary>
    /// 詳細 PDF 印刷用 ViewModel。
    /// </summary>
    public class DatabaseSamplePdfDetailViewModel
    {
        public DateTime OutputDate { get; set; } = DateTime.Now;
        public SampleEntity Parent { get; set; } = new();
        public List<SampleEntityChild> Children { get; set; } = new();
        public List<string> NonImageFiles { get; set; } = new();
        public List<DatabaseSamplePdfImageViewModel> Images { get; set; } = new();
    }

    /// <summary>
    /// 詳細 PDF に埋め込む画像データ。
    /// </summary>
    public class DatabaseSamplePdfImageViewModel
    {
        public string FileName { get; set; } = "";
        public string DataUri { get; set; } = "";
    }

    // ─────────────────────────────────────────────
    // 子エンティティ 新規作成・編集用 ViewModel
    // ─────────────────────────────────────────────

    // ポイント: Id が null → 新規作成、値あり → 編集 として Controller/View で判定する
    public class DatabaseSampleChildEditViewModel
    {
        public long? Id { get; set; }

        /// <summary>親エンティティのID（SumpleEntityID に対応）</summary>
        [Required]
        public long ParentId { get; set; }

        [Display(Name = "文字列データ")]
        [Required(ErrorMessage = "文字列データは必須です。")]
        [MaxLength(200)]
        public string StringData { get; set; } = "";

        [Display(Name = "数値データ")]
        public int IntData { get; set; }

        [Display(Name = "BoolData")]
        public bool BoolData { get; set; }
    }

    public class DatabaseMapperUsageSampleDataModel
    {
        public string? ApplicationUserId { get; set; }
        // ポイント: RecognizeDestinationPrefixes("MapperUsage") により
        //           AutoMapper が SampleEntity.StringData → MapperUsageStringData にマッピングする
        public string? MapperUsageStringData { get; set; }
        public int MapperUsageIntData { get; set; }
        public bool MapperUsageBoolData { get; set; }
        public SampleEnum MapperUsageEnumData { get; set; }
        // ポイント: RecognizeDestinationPostfixes("WithMapper") により
        //           AutoMapper が SampleEntity.StringData → StringDataWithMapper にマッピングする
        public string? StringDataWithMapper { get; set; }
        public int IntDataWithMapper { get; set; }
        public bool BoolDataWithMapper { get; set; }
        public SampleEnum EnumDataWithMapper { get; set; }
        // ポイント: 名前が異なるプロパティは ForMember で個別に指定する（UpdateApplicationUserId → LastUpdatedBy）
        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
