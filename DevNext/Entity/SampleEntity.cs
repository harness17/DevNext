using Dev.CommonLibrary.Entity;
using Site.Common;
using System.ComponentModel.DataAnnotations;

namespace Site.Entity
{
    public class SampleEntity : SampleEntityBase { }

    public class SampleEntityHistory : SampleEntityBase, IEntityHistory
    {
        [Key]
        public long HistoryId { get; set; }
    }

    public abstract class SampleEntityBase : SiteEntityBase
    {
        [MaxLength(128)]
        public string? ApplicationUserId { get; set; }

        [Required]
        public string StringData { get; set; } = "";

        public int IntData { get; set; }
        public bool BoolData { get; set; }
        public SampleEnum EnumData { get; set; }
        public SampleEnum2 EnumData2 { get; set; }
        public string? FileData { get; set; }
    }
}
