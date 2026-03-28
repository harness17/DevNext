namespace Site.Entity.Enums
{
    /// <summary>
    /// スケジュールイベント繰り返し種別
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>なし（繰り返しなし）</summary>
        None = 0,

        /// <summary>毎日</summary>
        Daily = 1,

        /// <summary>毎週</summary>
        Weekly = 2,

        /// <summary>毎月</summary>
        Monthly = 3,

        /// <summary>毎年</summary>
        Yearly = 4
    }
}
