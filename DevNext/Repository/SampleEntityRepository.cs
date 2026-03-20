using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Repository
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
        private readonly DBContext _context;

        public SampleEntityRepository(DBContext context) : base(context)
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
                    "IntData" => query.OrderByDescending(s => s.IntData),
                    "BoolData" => query.OrderByDescending(s => s.BoolData),
                    "EnumData" => query.OrderByDescending(s => s.EnumData),
                    _ => query.OrderByDescending(s => s.Id)
                };
            }
            else
            {
                query = cond.Pager.sort switch
                {
                    "StringData" => query.OrderBy(s => s.StringData),
                    "IntData" => query.OrderBy(s => s.IntData),
                    "BoolData" => query.OrderBy(s => s.BoolData),
                    "EnumData" => query.OrderBy(s => s.EnumData),
                    _ => query.OrderBy(s => s.Id)
                };
            }

            // ポイント: Count() を先に実行してから Take/Skip することでページング総件数を取得する
            //           Count() はDBへのSQLクエリ（COUNT文）が実行される
            int totalRecords = query.Count();
            // Take（件数）と Skip（オフセット）を適用してページ分のデータだけ取得する
            LocalUtil.SetTakeSkip(ref query, cond);
            model.rows = query.ToList();  // ここで実際のSQLが発行される
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        public IQueryable<SampleEntity> GetSampleEntityQuery(SampleEntityCondViewModel? cond = null)
        {
            return GetBaseQuery(cond);
        }

        // ポイント: GetCondModel は View 用 ViewModel（DatabaseSampleCondViewModel）を
        //           Repository 用 CondModel（SampleEntityCondViewModel）に AutoMapper で変換する
        //           EnumData は List<string> → List<SampleEnum?> への変換が必要なため個別にマッピング定義
        public override SampleEntityCondViewModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config = config ?? new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleCondViewModel, SampleEntityCondViewModel>()
                   // ポイント: EnumData の文字列リストを Enum 値リストに変換する
                   //           SampleEnumParser を static メソッドとして分離することで LINQ 式内で使用可能になる
                   .ForMember(d => d.EnumData, o => o.MapFrom(s => s.EnumData.Select(x => SampleEnumParser.Parse(x)).ToList())),
                NullLoggerFactory.Instance);
            return config.CreateMapper().Map<SampleEntityCondViewModel>(viewCondModel);
        }

        // ポイント: GetBaseQuery は論理削除フィルタ + 条件を IQueryable として返す
        //           IQueryable のまま返すことで呼び出し側でさらにソート・ページングを追加できる（遅延評価）
        //           includeDelete=true にすると論理削除済みデータも含めて取得できる
        public override IQueryable<SampleEntity> GetBaseQuery(SampleEntityCondViewModel? cond = null, bool includeDelete = false)
        {
            // ポイント: 論理削除フィルタを最初に適用して全体に効かせる
            IQueryable<SampleEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                // ポイント: null チェックしてから Where を連鎖するパターン
                //           条件が指定されていない場合はフィルタを追加しない（全件取得になる）
                if (cond.Id != null) query = query.Where(x => x.Id == cond.Id);
                if (!string.IsNullOrEmpty(cond.ApplicationUserId)) query = query.Where(x => x.ApplicationUserId == cond.ApplicationUserId);
                if (!string.IsNullOrEmpty(cond.StringData)) query = query.Where(x => x.StringData.Contains(cond.StringData));  // 部分一致検索
                if (cond.IntData != null) query = query.Where(x => x.IntData == cond.IntData);
                if (cond.BoolData != null) query = query.Where(x => x.BoolData == cond.BoolData);
                // ポイント: Enum の複数選択（チェックボックス）は IN 句に相当する Any() で実装
                if (cond.EnumData != null && cond.EnumData.Count != 0 && cond.EnumData.Any(x => x != null))
                    query = query.Where(x => cond.EnumData.Any(key => x.EnumData == key));
                if (cond.EnumData2 != null) query = query.Where(x => x.EnumData2 == cond.EnumData2);
                // 日付の範囲検索（開始・終了は独立してオプション指定できる）
                if (cond.Create_start_date != null) query = query.Where(x => cond.Create_start_date <= x.CreateDate);
                if (cond.Create_end_date != null) query = query.Where(x => cond.Create_end_date >= x.CreateDate);
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

    // 以下はJoin・GroupJoinのサンプル用モデル（使用例は CommonLibrary/Repository 参照）
    public class SampleEntityJoinSampleModel
    {
        public SampleEntity? Sample { get; set; }
        public SampleEntityChild? Child { get; set; }
    }

    public class SampleEntityGroupJoinSampleModel
    {
        public SampleEntity? Sample { get; set; }
        public List<SampleEntityChild> Child { get; set; } = new();
    }
}
