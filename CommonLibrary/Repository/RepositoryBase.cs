using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dev.CommonLibrary.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;

namespace Dev.CommonLibrary.Repository
{
    /// <summary>
    /// リポジトリ親クラス
    /// </summary>
    public abstract class RepositoryBase<TEntity, TCondModel> : IRepository<TEntity, TCondModel>
        where TEntity : class, IEntity
        where TCondModel : class, IRepositoryCondModel
    {
        protected DbContext context;
        protected DbSet<TEntity> dbSet;

        public RepositoryBase(DbContext context)
        {
            this.context = context;
            this.dbSet = context.Set<TEntity>();
        }

        public virtual TEntity? SelectById(object id) => dbSet.Find(id);

        public List<TEntity> Select(Expression<Func<TEntity, bool>>? filter = null)
        {
            IQueryable<TEntity> query = dbSet;
            if (filter != null) query = query.Where(filter);
            return query.ToList();
        }

        public virtual TEntity Insert(TEntity entity, bool isSaveChanges = true)
        {
            entity.SetForCreate();
            return InsertSimple(entity, isSaveChanges);
        }

        public virtual TEntity InsertSimple(TEntity entity, bool isSaveChanges = true)
        {
            dbSet.Add(entity);
            if (isSaveChanges) context.SaveChanges();
            return entity;
        }

        public virtual void BatchInsert(IEnumerable<TEntity> insertlist)
        {
            context.AddRange(insertlist);
            context.SaveChanges();
        }

        public virtual TEntity Update(TEntity entity, bool isSaveChanges = true)
        {
            entity.SetForUpdate();
            return UpdateSimple(entity, isSaveChanges);
        }

        public virtual TEntity UpdateSimple(TEntity entity, bool isSaveChanges = true)
        {
            dbSet.Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            if (isSaveChanges) context.SaveChanges();
            return entity;
        }

        public virtual TEntity? PhysicalDelete(object id, bool isSaveChanges = true)
        {
            TEntity? entity = dbSet.Find(id);
            if (entity == null) return null;
            return PhysicalDelete(entity, isSaveChanges);
        }

        public virtual TEntity PhysicalDelete(TEntity entity, bool isSaveChanges = true)
        {
            if (context.Entry(entity).State == EntityState.Detached)
                dbSet.Attach(entity);
            dbSet.Remove(entity);
            if (isSaveChanges) context.SaveChanges();
            return entity;
        }

        public virtual TEntity LogicalDelete(object id, bool isSaveChanges = true)
        {
            TEntity entity = dbSet.Find(id)!;
            return LogicalDelete(entity, isSaveChanges);
        }

        public virtual TEntity LogicalDelete(TEntity entity, bool isSaveChanges = true)
        {
            entity.SetForLogicalDelete();
            dbSet.Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
            if (isSaveChanges) context.SaveChanges();
            return entity;
        }

        public virtual void PhysicalDeletes(Expression<Func<TEntity, bool>> filter, bool isSaveChanges = true)
        {
            var entities = dbSet.Where(filter).ToList();
            dbSet.RemoveRange(entities);
            if (isSaveChanges) context.SaveChanges();
        }

        public virtual void LogicalDeletes(List<TEntity> entities, bool isSaveChanges = true)
        {
            foreach (var entity in entities) LogicalDelete(entity, false);
            if (isSaveChanges) context.SaveChanges();
        }

        public abstract IQueryable<TEntity> GetBaseQuery(TCondModel? cond = null, bool includeDelete = false);

        public virtual IQueryable<TModel> GetQueryAs<TModel>(TCondModel? cond = null, MapperConfiguration? config = null)
        {
            config = config ?? new MapperConfiguration(cfg => cfg.CreateMap<TEntity, TModel>(), NullLoggerFactory.Instance);
            return GetBaseQuery(cond).ProjectTo<TModel>(config);
        }

        public virtual TCondModel GetCondModel<T>(T viewCondModel, MapperConfiguration? config = null)
        {
            config = config ?? new MapperConfiguration(cfg => cfg.CreateMap<T, TCondModel>(), NullLoggerFactory.Instance);
            return config.CreateMapper().Map<TCondModel>(viewCondModel);
        }

        public void SaveChanges() => context.SaveChanges();
    }

    /// <summary>
    /// リポジトリ親クラス（履歴あり）
    /// </summary>
    public abstract class RepositoryBase<TEntity, TEntityHistory, TCondModel>
        : RepositoryBase<TEntity, TCondModel>, IRepositoryHistory<TEntity, TEntityHistory>
        where TEntity : class, IEntity
        where TEntityHistory : class, IEntityHistory
        where TCondModel : class, IRepositoryCondModel
    {
        protected DbSet<TEntityHistory> dbSetHistory;

        public RepositoryBase(DbContext context) : base(context)
        {
            this.dbSetHistory = context.Set<TEntityHistory>();
        }

        public override TEntity InsertSimple(TEntity entity, bool isSaveChanges = true)
        {
            entity = base.InsertSimple(entity, isSaveChanges);
            InsertHistory(entity, isSaveChanges);
            return entity;
        }

        public override TEntity UpdateSimple(TEntity entity, bool isSaveChanges = true)
        {
            entity = base.UpdateSimple(entity, isSaveChanges);
            InsertHistory(entity, isSaveChanges);
            return entity;
        }

        public override TEntity LogicalDelete(TEntity entity, bool isSaveChanges = true)
        {
            base.LogicalDelete(entity, isSaveChanges);
            InsertHistory(entity, isSaveChanges);
            return entity;
        }

        public TEntityHistory InsertHistory(TEntity entity, bool isSaveChanges = true)
        {
            TEntityHistory? entityHistory = null;
            var mapper = new MapperConfiguration(cfg => cfg.CreateMap<TEntity, TEntityHistory>(), NullLoggerFactory.Instance).CreateMapper();
            entityHistory = mapper.Map(entity, entityHistory);
            dbSetHistory.Add(entityHistory!);
            if (isSaveChanges) context.SaveChanges();
            return entityHistory!;
        }
    }
}
