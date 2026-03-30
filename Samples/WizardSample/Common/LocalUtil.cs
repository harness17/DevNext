using AutoMapper;
using WizardSample.Models;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace WizardSample.Common
{
    /// <summary>
    /// WizardSample プロジェクト共通ユーティリティ
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
    }
}
