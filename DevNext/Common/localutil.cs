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
        /// <summary>一覧画面の表示件数選択ドロップダウン用リストを返す（10/20/30 件）。</summary>
        public static IEnumerable<SelectListItem> SetRecoedNumberList()
        {
            return new SelectListItem[]
            {
                new SelectListItem { Text = "10件", Value = "10" },
                new SelectListItem { Text = "20件", Value = "20" },
                new SelectListItem { Text = "30件", Value = "30" },
            };
        }

        /// <summary>クエリにページングの Skip/Take を適用する。recoedNumber が 0 のときは全件取得。</summary>
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

        /// <summary>現在のカルチャに応じて多言語文字列の日本語または英語を返す。</summary>
        public static string MultiLangStr(IMultipleLanguagesString? s)
        {
            if (s == null) return "";
            if (CultureInfo.CurrentCulture.Parent.IetfLanguageTag.ToUpper() == LanguageMin.en.ToString().ToUpper())
                return s.En ?? "";
            return s.Ja ?? "";
        }

        /// <summary>登録完了アラートメッセージを生成する。</summary>
        public static string GetCreateAlertMessage(string title) => GetAlertMessage("{1}を登録しました。", title);
        /// <summary>更新完了アラートメッセージを生成する。</summary>
        public static string GetUpdateAlertMessage(string title) => GetAlertMessage("{1}を更新しました。", title);
        /// <summary>削除完了アラートメッセージを生成する。</summary>
        public static string GetDeleteAlertMessage(string title) => GetAlertMessage("{1}を削除しました。", title);
        /// <summary>処理失敗アラートメッセージを生成する。</summary>
        public static string GetErrorAlertMessage(string title) => GetAlertMessage("{1}の処理に失敗しました。", title);
        /// <summary>テンプレートに title を埋め込んだアラートメッセージを返す。</summary>
        public static string GetAlertMessage(string template, string title) => string.Format(template, title, title);

        /// <summary>SearchModelBase のページング状態を任意の ISearchModelBase 実装型へマッピングして返す。</summary>
        public static T MapPageModelTo<T>(SearchModelBase? pageModel) where T : ISearchModelBase, new()
        {
            var model = new T();
            if (pageModel == null) return model;
            var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SearchModelBase, T>(), NullLoggerFactory.Instance).CreateMapper();
            return mapper.Map(pageModel, model);
        }

        /// <summary>PageRead の種別に応じてページャー状態（ページ番号・件数・ソート）を更新する。</summary>
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
