using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using PdfSample.Common;

namespace PdfSample.Models;

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
    public CommonListPagerModel Pager { get; set; } = new();
}
