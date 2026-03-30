using AutoMapper;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using FileSample.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileSample.Common
{
    /// <summary>
    /// FileSample プロジェクト共通ユーティリティ
    /// </summary>
    public static class LocalUtil
    {
        /// <summary>表示件数ドロップダウン用リストを生成する</summary>
        public static IEnumerable<SelectListItem> SetRecoedNumberList()
        {
            return new SelectListItem[]
            {
                new SelectListItem { Text = "10件", Value = "10" },
                new SelectListItem { Text = "20件", Value = "20" },
                new SelectListItem { Text = "30件", Value = "30" },
            };
        }

        /// <summary>
        /// IQueryable にページング（Skip/Take）を適用する
        /// </summary>
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

        /// <summary>登録完了メッセージを返す</summary>
        public static string GetCreateAlertMessage(string title) => GetAlertMessage("{1}を登録しました。", title);
        /// <summary>更新完了メッセージを返す</summary>
        public static string GetUpdateAlertMessage(string title) => GetAlertMessage("{1}を更新しました。", title);
        /// <summary>削除完了メッセージを返す</summary>
        public static string GetDeleteAlertMessage(string title) => GetAlertMessage("{1}を削除しました。", title);
        /// <summary>エラーメッセージを返す</summary>
        public static string GetErrorAlertMessage(string title) => GetAlertMessage("{1}の処理に失敗しました。", title);

        private static string GetAlertMessage(string template, string title) =>
            string.Format(template, title, title);

        /// <summary>
        /// SearchModelBase のページ・ソート情報を任意の ViewModel（ISearchModelBase）にコピーする
        /// </summary>
        public static T MapPageModelTo<T>(SearchModelBase? pageModel) where T : ISearchModelBase, new()
        {
            var model = new T();
            if (pageModel == null) return model;
            var mapper = new MapperConfiguration(
                cfg => cfg.CreateMap<SearchModelBase, T>(),
                NullLoggerFactory.Instance).CreateMapper();
            return mapper.Map(pageModel, model);
        }

        /// <summary>
        /// ページャー設定（ページ番号・件数・ソート列）を CondModel に反映する
        /// ポイント: PageRead の種別によって更新するプロパティを切り替える
        /// </summary>
        public static void SetPager(SearchCondModelBase? cond, ISearchModelBase baseModel)
        {
            if (cond == null) cond = new SearchCondModelBase();
            if (cond.Pager == null)
            {
                cond.Pager = new CommonListPagerModel(
                    baseModel.Page, baseModel.Sort, baseModel.SortDir, baseModel.RecordNum);
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
