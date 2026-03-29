using System.ComponentModel.DataAnnotations;

namespace DatabaseSample.Common
{
    // DatabaseSample で使用する列挙型

    public enum PageRead
    {
        Resarch,
        Paging,
        Sorting,
        ChangeRecordNum
    }

    public enum SampleEnum
    {
        [Display(Name = "選択肢1")]
        select1 = 0,
        [Display(Name = "選択肢2")]
        select2 = 2,
        [Display(Name = "選択肢3")]
        select3 = 3
    }

    public enum SampleEnum2
    {
        [Display(Name = "選択肢21")]
        select21 = 0,
        [Display(Name = "選択肢22")]
        select22 = 2,
        [Display(Name = "選択肢23")]
        select23 = 3
    }
}
