using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using Site.Common;
using Site.Entity;
using Site.Models;

namespace Site.Repository
{
    internal static class SampleEnumParser
    {
        public static SampleEnum? Parse(string? x)
        {
            if (string.IsNullOrEmpty(x)) return null;
            return Enum.TryParse<SampleEnum>(x, out var e) ? (SampleEnum?)e : null;
        }
    }

    public class SampleEntityRepository : RepositoryBase<SampleEntity, SampleEntityHistory, SampleEntityCondViewModel>
    {
        private readonly DBContext _context;

        public SampleEntityRepository(DBContext context) : base(context)
        {
            _context = context;
        }

        public DatabaseSampleDataViewModel GetSampleEntityList(SampleEntityCondViewModel cond)
        {
            var model = new DatabaseSampleDataViewModel();
            IQueryable<SampleEntity> query = GetSampleEntityQuery(cond);

            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

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

            int totalRecords = query.Count();
            localutil.SetTakeSkip(ref query, cond);
            model.rows = query.ToList();
            model.Summary = util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        public IQueryable<SampleEntity> GetSampleEntityQuery(SampleEntityCondViewModel? cond = null)
        {
            return GetBaseQuery(cond);
        }

        public override SampleEntityCondViewModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config = config ?? new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleCondViewModel, SampleEntityCondViewModel>()
                   .ForMember(d => d.EnumData, o => o.MapFrom(s => s.EnumData.Select(x => SampleEnumParser.Parse(x)).ToList())),
                NullLoggerFactory.Instance);
            return config.CreateMapper().Map<SampleEntityCondViewModel>(viewCondModel);
        }

        public override IQueryable<SampleEntity> GetBaseQuery(SampleEntityCondViewModel? cond = null, bool includeDelete = false)
        {
            IQueryable<SampleEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (cond.Id != null) query = query.Where(x => x.Id == cond.Id);
                if (!string.IsNullOrEmpty(cond.ApplicationUserId)) query = query.Where(x => x.ApplicationUserId == cond.ApplicationUserId);
                if (!string.IsNullOrEmpty(cond.StringData)) query = query.Where(x => x.StringData.Contains(cond.StringData));
                if (cond.IntData != null) query = query.Where(x => x.IntData == cond.IntData);
                if (cond.BoolData != null) query = query.Where(x => x.BoolData == cond.BoolData);
                if (cond.EnumData != null && cond.EnumData.Count != 0 && cond.EnumData.Any(x => x != null))
                    query = query.Where(x => cond.EnumData.Any(key => x.EnumData == key));
                if (cond.EnumData2 != null) query = query.Where(x => x.EnumData2 == cond.EnumData2);
                if (cond.Create_start_date != null) query = query.Where(x => cond.Create_start_date <= x.CreateDate);
                if (cond.Create_end_date != null) query = query.Where(x => cond.Create_end_date >= x.CreateDate);
            }

            return query;
        }
    }

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
        public Dev.CommonLibrary.Common.CommonListPagerModel Pager { get; set; } = new(1, "Id", "ASC", 10);
    }

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
