using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;

namespace Site.Models
{
    public class FileData
    {
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }

    public interface IMultipleLanguagesString
    {
        string? Ja { get; set; }
        string? En { get; set; }
    }

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

    public class SearchCondModelBase : IRepositoryCondModel
    {
        public CommonListPagerModel Pager { get; set; } = new CommonListPagerModel();
    }
}
