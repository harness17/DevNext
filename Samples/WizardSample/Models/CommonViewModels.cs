using WizardSample.Common;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;

namespace WizardSample.Models
{
    /// <summary>
    /// 一覧画面 ViewModel の基底クラス。ページング・ソートに必要なプロパティを提供する。
    /// </summary>
    public interface ISearchModelBase
    {
        int Page { get; set; }
        string Sort { get; set; }
        string SortDir { get; set; }
        int RecordNum { get; set; }
        PageRead? PageRead { get; set; }
    }

    public class SearchModelBase : ISearchModelBase
    {
        public int Page { get; set; } = 1;
        public string Sort { get; set; } = "";
        public string SortDir { get; set; } = "ASC";
        public int RecordNum { get; set; } = 10;
        public PageRead? PageRead { get; set; }
    }

    /// <summary>
    /// 検索条件 ViewModel の基底クラス。ページング情報を持つ。
    /// </summary>
    public class SearchCondModelBase : IRepositoryCondModel
    {
        public CommonListPagerModel Pager { get; set; } = new CommonListPagerModel();
    }
}
