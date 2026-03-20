using Dev.CommonLibrary.Entity;

namespace Site.Entity
{
    /// <summary>
    /// サイトエンティティベース
    /// </summary>
    public abstract class SiteEntityBase : EntityBase, IEntity
    {
        public long Id { get; set; }
    }
}
