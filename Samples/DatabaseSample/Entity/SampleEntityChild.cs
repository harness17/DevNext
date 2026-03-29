using Dev.CommonLibrary.Entity;
using System.ComponentModel.DataAnnotations;

namespace DatabaseSample.Entity
{
    public class SampleEntityChild : SampleEntityChildBase { }

    public class SampleEntityChildHistory : SampleEntityChildBase, IEntityHistory
    {
        [Key]
        public long HistoryId { get; set; }
    }

    public abstract class SampleEntityChildBase : SiteEntityBase
    {
        public long SumpleEntityID { get; set; }

        [MaxLength(128)]
        public string? ApplicationUserId { get; set; }

        [Required]
        public string StringData { get; set; } = "";

        public int IntData { get; set; }
        public bool BoolData { get; set; }
    }
}
