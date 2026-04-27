using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;

namespace Site.Models
{
    /// <summary>ファイルアップロード結果を保持する汎用モデル。</summary>
    public class FileData
    {
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }

    /// <summary>多言語対応文字列プロパティを持つモデルのインターフェース。</summary>
    public interface IMultipleLanguagesString
    {
        string? Ja { get; set; }
        string? En { get; set; }
    }

    /// <summary>一覧画面のページング・ソート状態を保持するインターフェース。</summary>
    public interface ISearchModelBase
    {
        int Page { get; set; }
        string Sort { get; set; }
        string SortDir { get; set; }
        int RecordNum { get; set; }
        PageRead? PageRead { get; set; }
    }

    /// <summary>一覧画面の検索条件・ページング状態の基底 ViewModel。</summary>
    public class SearchModelBase : ISearchModelBase
    {
        public int Page { get; set; } = 1;
        public string Sort { get; set; } = "";
        public string SortDir { get; set; } = "ASC";
        public int RecordNum { get; set; } = 10;
        public PageRead? PageRead { get; set; }
    }

    /// <summary>リポジトリ検索条件の基底クラス。ページャー情報を保持する。</summary>
    public class SearchCondModelBase : IRepositoryCondModel
    {
        public CommonListPagerModel Pager { get; set; } = new CommonListPagerModel();
    }
}
