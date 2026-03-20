using Site.Common;

namespace Site.Models
{
    public class PartialSampleViewModel
    {
        public PartialSampleChildModel Child { get; set; } = new PartialSampleChildModel();
        public List<PartialSampleChildModel> ChildList { get; set; } = new List<PartialSampleChildModel>();
    }

    public class PartialSampleChildModel
    {
        public string? Text { get; set; }
        public int Number { get; set; }
        public int Order { get; set; }
        public SampleEnum Enum { get; set; }
    }
}
