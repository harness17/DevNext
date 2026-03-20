using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Site.Models;
using System.ComponentModel;
using System.Globalization;

namespace Site.Common
{
    /// <summary>
    /// 共通関数クラス
    /// </summary>
    public static class LocalUtil
    {
        public static IEnumerable<SelectListItem> SetRecoedNumberList()
        {
            return new SelectListItem[]
            {
                new SelectListItem { Text = "10件", Value = "10" },
                new SelectListItem { Text = "20件", Value = "20" },
                new SelectListItem { Text = "30件", Value = "30" },
            };
        }

        public static void SetTakeSkip<TModel, CondModel>(ref IQueryable<TModel> query, CondModel cond)
            where CondModel : IRepositoryCondModel
        {
            if (cond.Pager.recoedNumber != 0)
            {
                int takeNumber = cond.Pager.recoedNumber;
                int skipNumber = (cond.Pager.page - 1) * takeNumber;
                query = query.Skip(skipNumber).Take(takeNumber);
            }
        }

        public static string MultiLangStr(IMultipleLanguagesString? s)
        {
            if (s == null) return "";
            if (CultureInfo.CurrentCulture.Parent.IetfLanguageTag.ToUpper() == LanguageMin.en.ToString().ToUpper())
                return s.En ?? "";
            return s.Ja ?? "";
        }

        public static string GetCreateAlertMessage(string title) => GetAlertMessage("{1}を登録しました。", title);
        public static string GetUpdateAlertMessage(string title) => GetAlertMessage("{1}を更新しました。", title);
        public static string GetDeleteAlertMessage(string title) => GetAlertMessage("{1}を削除しました。", title);
        public static string GetErrorAlertMessage(string title) => GetAlertMessage("{1}の処理に失敗しました。", title);
        public static string GetAlertMessage(string template, string title) => string.Format(template, title, title);

        public static T MapPageModelTo<T>(SearchModelBase? pageModel) where T : ISearchModelBase, new()
        {
            var model = new T();
            if (pageModel == null) return model;
            var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SearchModelBase, T>(), NullLoggerFactory.Instance).CreateMapper();
            return mapper.Map(pageModel, model);
        }

        public static void SetPager(SearchCondModelBase? cond, ISearchModelBase baseModel)
        {
            if (cond == null) cond = new SearchCondModelBase();
            if (cond.Pager == null)
            {
                cond.Pager = new CommonListPagerModel(baseModel.Page, baseModel.Sort, baseModel.SortDir, baseModel.RecordNum);
            }
            else
            {
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
                    case PageRead.Resarch:
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
    }
}
