using AutoMapper;
using DatabaseSample.Common;
using DatabaseSample.Data;
using DatabaseSample.Entity;
using DatabaseSample.Models;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Microsoft.Extensions.Logging.Abstractions;

namespace DatabaseSample.Repository
{
    // ポイント: Enum の文字列→値変換をリポジトリ内の static クラスにまとめる
    //           AutoMapper の MapFrom で LINQ 式として渡せるようにするための分離
    internal static class SampleEnumParser
    {
        public static SampleEnum? Parse(string? x)
        {
            if (string.IsNullOrEmpty(x)) return null;
            return Enum.TryParse<SampleEnum>(x, out var e) ? (SampleEnum?)e : null;
        }
    }

    // ポイント: RepositoryBase<TEntity, THistory, TCond> を継承することで
    //           Insert / Update / LogicalDelete / SelectById / BatchInsert 等の共通CRUD操作を継承する
    //           プロジェクト固有の検索処理だけこのクラスに追加する設計
    public class SampleEntityRepository : RepositoryBase<SampleEntity, SampleEntityHistory, SampleEntityCondViewModel>
    {
        private readonly DatabaseSampleDbContext _context;

        public SampleEntityRepository(DatabaseSampleDbContext context) : base(context)
        {
            _context = context;
        }

        // ポイント: 一覧取得はソート・ページング付きで DatabaseSampleDataViewModel を返す
        public DatabaseSampleDataViewModel GetSampleEntityList(SampleEntityCondViewModel cond)
        {
            var model = new DatabaseSampleDataViewModel();
            IQueryable<SampleEntity> query = GetSampleEntityQuery(cond);

            // デフォルトソートは Id 降順
            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            // ポイント: switch 式でソート列・方向を切り替える
            //           パターンマッチングにより可読性が高く、追加も容易
            if (cond.Pager.sortdir.ToLower() == "desc")
            {
                query = cond.Pager.sort switch
                {
                    "StringData" => query.OrderByDescending(s => s.StringData),
                    "IntData"    => query.OrderByDescending(s => s.IntData),
                    "BoolData"   => query.OrderByDescending(s => s.BoolData),
                    "EnumData"   => query.OrderByDescending(s => s.EnumData),
                    _            => query.OrderByDescending(s => s.Id)
                };
            }
            else
            {
                query = cond.Pager.sort switch
                {
                    "StringData" => query.OrderBy(s => s.StringData),
                    "IntData"    => query.OrderBy(s => s.IntData),
                    "BoolData"   => query.OrderBy(s => s.BoolData),
                    "EnumData"   => query.OrderBy(s => s.EnumData),
                    _            => query.OrderBy(s => s.Id)
                };
            }

            // ポイント: Count() を先に実行してから Take/Skip することでページング総件数を取得する
            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.rows = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        public IQueryable<SampleEntity> GetSampleEntityQuery(SampleEntityCondViewModel? cond = null)
        {
            return GetBaseQuery(cond);
        }

        // ポイント: GetCondModel は View 用 ViewModel（DatabaseSampleCondViewModel）を
        //           Repository 用 CondModel（SampleEntityCondViewModel）に AutoMapper で変換する
        public override SampleEntityCondViewModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config = config ?? new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleCondViewModel, SampleEntityCondViewModel>()
                   // ポイント: EnumData の文字列リストを Enum 値リストに変換する
                   .ForMember(d => d.EnumData,
                       o => o.MapFrom(s => s.EnumData.Select(x => SampleEnumParser.Parse(x)).ToList())),
                NullLoggerFactory.Instance);
            return config.CreateMapper().Map<SampleEntityCondViewModel>(viewCondModel);
        }

        // ポイント: GetBaseQuery は論理削除フィルタ + 条件を IQueryable として返す
        //           IQueryable のまま返すことで呼び出し側でさらにソート・ページングを追加できる（遅延評価）
        public override IQueryable<SampleEntity> GetBaseQuery(
            SampleEntityCondViewModel? cond = null, bool includeDelete = false)
        {
            IQueryable<SampleEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (cond.Id != null)                         query = query.Where(x => x.Id == cond.Id);
                if (!string.IsNullOrEmpty(cond.ApplicationUserId))
                    query = query.Where(x => x.ApplicationUserId == cond.ApplicationUserId);
                if (!string.IsNullOrEmpty(cond.StringData))  query = query.Where(x => x.StringData.Contains(cond.StringData));
                if (cond.IntData != null)                    query = query.Where(x => x.IntData == cond.IntData);
                if (cond.BoolData != null)                   query = query.Where(x => x.BoolData == cond.BoolData);
                // ポイント: Enum の複数選択（チェックボックス）は IN 句に相当する Any() で実装
                if (cond.EnumData != null && cond.EnumData.Count != 0 && cond.EnumData.Any(x => x != null))
                    query = query.Where(x => cond.EnumData.Any(key => x.EnumData == key));
                if (cond.EnumData2 != null)                  query = query.Where(x => x.EnumData2 == cond.EnumData2);
                if (cond.Create_start_date != null)          query = query.Where(x => cond.Create_start_date <= x.CreateDate);
                if (cond.Create_end_date != null)            query = query.Where(x => cond.Create_end_date >= x.CreateDate);
            }

            return query;
        }
    }

    // ポイント: Repository 専用の条件モデル（IRepositoryCondModel を実装）
    //           View 用 ViewModel と分離することで、DB 検索ロジックを View の都合から切り離せる
    public class SampleEntityCondViewModel : IRepositoryCondModel
    {
        public long? Id { get; set; }
        public string? ApplicationUserId { get; set; }
        public string? StringData { get; set; }
        public int? IntData { get; set; }
        public bool? BoolData { get; set; }
        public List<SampleEnum?> EnumData { get; set; } = new();
        public SampleEnum2? EnumData2 { get; set; }
        public DateTime? Create_start_date { get; set; }
        public DateTime? Create_end_date { get; set; }
        // ポイント: Pager はページング・ソート情報を保持する共通モデル（共通ライブラリで定義）
        public Dev.CommonLibrary.Common.CommonListPagerModel Pager { get; set; } = new(1, "Id", "ASC", 10);
    }
}
