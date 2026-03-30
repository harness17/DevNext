using AutoMapper;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.Extensions.Logging.Abstractions;
using WizardSample.Common;
using WizardSample.Data;
using WizardSample.Entity;
using WizardSample.Models;

namespace WizardSample.Repository
{
    /// <summary>
    /// ウィザードエンティティリポジトリ
    /// 多段階フォームの完了データを保存・取得する
    /// </summary>
    public class WizardEntityRepository : RepositoryBase<WizardEntity, WizardEntityCondModel>
    {
        public WizardEntityRepository(WizardSampleDbContext context) : base(context) { }

        /// <summary>
        /// 検索条件を適用してウィザード登録データ一覧を取得する
        /// </summary>
        public WizardSampleDataViewModel GetWizardEntityList(WizardEntityCondModel cond)
        {
            var model = new WizardSampleDataViewModel();
            IQueryable<WizardEntity> query = GetBaseQuery(cond);

            // デフォルトソートは登録日時降順（新しい順）
            cond.Pager.sort    = string.IsNullOrEmpty(cond.Pager.sort)    ? "Id"   : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            query = cond.Pager.sortdir.ToLower() == "desc"
                ? cond.Pager.sort switch
                {
                    "Name"        => query.OrderByDescending(x => x.Name),
                    "Email"       => query.OrderByDescending(x => x.Email),
                    "Subject"     => query.OrderByDescending(x => x.Subject),
                    "DesiredDate" => query.OrderByDescending(x => x.DesiredDate),
                    "CreateDate"  => query.OrderByDescending(x => x.CreateDate),
                    _             => query.OrderByDescending(x => x.Id)
                }
                : cond.Pager.sort switch
                {
                    "Name"        => query.OrderBy(x => x.Name),
                    "Email"       => query.OrderBy(x => x.Email),
                    "Subject"     => query.OrderBy(x => x.Subject),
                    "DesiredDate" => query.OrderBy(x => x.DesiredDate),
                    "CreateDate"  => query.OrderBy(x => x.CreateDate),
                    _             => query.OrderBy(x => x.Id)
                };

            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.rows    = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        /// <summary>
        /// ViewModelの検索条件をリポジトリ用Condに変換する
        /// </summary>
        public override WizardEntityCondModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config ??= new MapperConfiguration(
                cfg => cfg.CreateMap<WizardSampleCondViewModel, WizardEntityCondModel>(),
                NullLoggerFactory.Instance);
            return config.CreateMapper().Map<WizardEntityCondModel>(viewCondModel);
        }

        /// <summary>
        /// 検索条件を組み立てるベースクエリ
        /// </summary>
        public override IQueryable<WizardEntity> GetBaseQuery(WizardEntityCondModel? cond = null, bool includeDelete = false)
        {
            IQueryable<WizardEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (!string.IsNullOrEmpty(cond.Name))
                    query = query.Where(x => x.Name.Contains(cond.Name));
                if (!string.IsNullOrEmpty(cond.Email))
                    query = query.Where(x => x.Email.Contains(cond.Email));
                if (cond.Category.HasValue)
                    query = query.Where(x => x.Category == cond.Category.Value);
            }

            return query;
        }
    }

    /// <summary>
    /// ウィザード検索条件（リポジトリ内部用）
    /// </summary>
    public class WizardEntityCondModel : IRepositoryCondModel
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public WizardCategory? Category { get; set; }
        public CommonListPagerModel Pager { get; set; } = new(1, "Id", "DESC", 10);
    }
}
