using AutoMapper;
using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Repository;
using FileSample.Common;
using FileSample.Data;
using FileSample.Entity;
using FileSample.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileSample.Repository
{
    /// <summary>
    /// ファイル管理リポジトリ
    /// </summary>
    public class FileEntityRepository : RepositoryBase<FileEntity, FileEntityCondModel>
    {
        public FileEntityRepository(FileSampleDbContext context) : base(context) { }

        /// <summary>
        /// 検索条件を適用してファイル一覧を取得する
        /// </summary>
        public FileManagementDataViewModel GetFileEntityList(FileEntityCondModel cond)
        {
            var model = new FileManagementDataViewModel();
            IQueryable<FileEntity> query = GetBaseQuery(cond);

            // デフォルトソートは ID 降順（新しい順）
            cond.Pager.sort = string.IsNullOrEmpty(cond.Pager.sort) ? "Id" : cond.Pager.sort;
            cond.Pager.sortdir = string.IsNullOrEmpty(cond.Pager.sortdir) ? "DESC" : cond.Pager.sortdir;

            query = cond.Pager.sortdir.ToLower() == "desc"
                ? cond.Pager.sort switch
                {
                    "OriginalFileName" => query.OrderByDescending(x => x.OriginalFileName),
                    "FileSize"         => query.OrderByDescending(x => x.FileSize),
                    "CreateDate"       => query.OrderByDescending(x => x.CreateDate),
                    _                  => query.OrderByDescending(x => x.Id)
                }
                : cond.Pager.sort switch
                {
                    "OriginalFileName" => query.OrderBy(x => x.OriginalFileName),
                    "FileSize"         => query.OrderBy(x => x.FileSize),
                    "CreateDate"       => query.OrderBy(x => x.CreateDate),
                    _                  => query.OrderBy(x => x.Id)
                };

            int totalRecords = query.Count();
            LocalUtil.SetTakeSkip(ref query, cond);
            model.rows = query.ToList();
            model.Summary = Util.CreateSummary(cond.Pager, totalRecords, "{0}件中 {1} - {2} を表示");
            return model;
        }

        /// <summary>
        /// ViewModelの検索条件をリポジトリ用Condに変換する
        /// </summary>
        public override FileEntityCondModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config ??= new MapperConfiguration(
                cfg => cfg.CreateMap<FileManagementCondViewModel, FileEntityCondModel>(),
                NullLoggerFactory.Instance);
            return config.CreateMapper().Map<FileEntityCondModel>(viewCondModel);
        }

        /// <summary>
        /// 検索条件を組み立てるベースクエリ
        /// </summary>
        public override IQueryable<FileEntity> GetBaseQuery(FileEntityCondModel? cond = null, bool includeDelete = false)
        {
            IQueryable<FileEntity> query = dbSet.Where(x => includeDelete ? true : !x.DelFlag);

            if (cond != null)
            {
                if (!string.IsNullOrEmpty(cond.OriginalFileName))
                    query = query.Where(x => x.OriginalFileName.Contains(cond.OriginalFileName));
                if (!string.IsNullOrEmpty(cond.Description))
                    query = query.Where(x => x.Description != null && x.Description.Contains(cond.Description));
            }

            return query;
        }
    }

    /// <summary>
    /// ファイル検索条件モデル（リポジトリ内部用）
    /// </summary>
    public class FileEntityCondModel : IRepositoryCondModel
    {
        public string? OriginalFileName { get; set; }
        public string? Description { get; set; }
        public CommonListPagerModel Pager { get; set; } = new(1, "Id", "DESC", 10);
    }
}
