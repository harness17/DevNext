using AutoMapper;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using PdfSample.Models;

namespace PdfSample.Common;

public static class LocalUtil
{
    public static IEnumerable<SelectListItem> SetRecordNumberList()
    {
        return
        [
            new SelectListItem { Text = "10件", Value = "10" },
            new SelectListItem { Text = "20件", Value = "20" },
            new SelectListItem { Text = "30件", Value = "30" },
        ];
    }

    public static void SetTakeSkip<TModel, TCondModel>(ref IQueryable<TModel> query, TCondModel cond)
        where TCondModel : IRepositoryCondModel
    {
        if (cond.Pager.recoedNumber != 0)
        {
            int takeNumber = cond.Pager.recoedNumber;
            int skipNumber = (cond.Pager.page - 1) * takeNumber;
            query = query.Skip(skipNumber).Take(takeNumber);
        }
    }

    public static string GetCreateAlertMessage(string title) => GetAlertMessage("{0}を登録しました。", title);
    public static string GetUpdateAlertMessage(string title) => GetAlertMessage("{0}を更新しました。", title);
    public static string GetDeleteAlertMessage(string title) => GetAlertMessage("{0}を削除しました。", title);
    public static string GetErrorAlertMessage(string title) => GetAlertMessage("{0}の処理に失敗しました。", title);

    private static string GetAlertMessage(string template, string title) => string.Format(template, title);

    public static T MapPageModelTo<T>(SearchModelBase? pageModel) where T : ISearchModelBase, new()
    {
        var model = new T();
        if (pageModel == null) return model;
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SearchModelBase, T>(), NullLoggerFactory.Instance).CreateMapper();
        return mapper.Map(pageModel, model);
    }

    public static void SetPager(SearchCondModelBase? cond, ISearchModelBase baseModel)
    {
        cond ??= new SearchCondModelBase();
        if (cond.Pager == null)
        {
            cond.Pager = new CommonListPagerModel(baseModel.Page, baseModel.Sort, baseModel.SortDir, baseModel.RecordNum);
            return;
        }

        switch (baseModel.PageRead)
        {
            case PageRead.Paging:
                cond.Pager.page = baseModel.Page;
                baseModel.RecordNum = cond.Pager.recoedNumber;
                break;
            case PageRead.ChangeRecordNum:
                cond.Pager.page = 1;
                cond.Pager.recoedNumber = baseModel.RecordNum;
                break;
            case PageRead.Research:
                baseModel.RecordNum = cond.Pager.recoedNumber;
                break;
            case PageRead.Sorting:
                cond.Pager.sort = baseModel.Sort;
                cond.Pager.sortdir = baseModel.SortDir;
                baseModel.RecordNum = cond.Pager.recoedNumber;
                break;
        }
    }
}
