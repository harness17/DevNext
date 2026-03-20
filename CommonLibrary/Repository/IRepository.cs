using Dev.CommonLibrary.Common;
using Dev.CommonLibrary.Entity;
using System.Linq.Expressions;

namespace Dev.CommonLibrary.Repository
{
    public interface IRepository<TEntity, TCondModel>
        where TEntity : class, IEntity
        where TCondModel : class, IRepositoryCondModel
    {
        TEntity? SelectById(object id);
        List<TEntity> Select(Expression<Func<TEntity, bool>>? filter = null);
        TEntity Insert(TEntity entity, bool isSaveChanges = false);
        TEntity InsertSimple(TEntity entity, bool isSaveChanges = false);
        TEntity Update(TEntity entity, bool isSaveChanges = false);
        TEntity UpdateSimple(TEntity entity, bool isSaveChanges = false);
        TEntity? PhysicalDelete(object id, bool isSaveChanges = true);
        TEntity PhysicalDelete(TEntity entity, bool isSaveChanges = false);
        TEntity LogicalDelete(TEntity entity, bool isSaveChanges = false);
        IQueryable<TModel> GetQueryAs<TModel>(TCondModel? cond = null, AutoMapper.MapperConfiguration? config = null);
        TCondModel GetCondModel<T>(T cond, AutoMapper.MapperConfiguration? config = null);
        IQueryable<TEntity> GetBaseQuery(TCondModel? cond = null, bool includeDelete = false);
    }

    public interface IRepositoryHistory<TEntity, TEntityHistory>
        where TEntity : class, IEntity
        where TEntityHistory : class, IEntityHistory
    {
        TEntityHistory InsertHistory(TEntity entity, bool isSaveChanges = false);
    }

    public interface IRepositoryCondModel
    {
        CommonListPagerModel Pager { get; set; }
    }
}
