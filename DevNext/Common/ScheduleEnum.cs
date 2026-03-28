namespace Site.Common
{
    /// <summary>
    /// 繰り返し種別
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>繰り返しなし</summary>
        None = 0,
        /// <summary>毎日</summary>
        Daily = 1,
        /// <summary>毎週（RecurrenceDaysOfWeek で曜日指定）</summary>
        Weekly = 2,
        /// <summary>毎月（開始日と同じ日）</summary>
        Monthly = 3
    }

    /// <summary>
    /// 参加者ステータス
    /// </summary>
    public enum ParticipantStatus
    {
        /// <summary>招待済み（未回答）</summary>
        Invited = 0,
        /// <summary>承諾</summary>
        Accepted = 1,
        /// <summary>辞退</summary>
        Declined = 2
    }
}
