namespace Site.Entity.Enums
{
    /// <summary>
    /// スケジュールイベント参加者ステータス
    /// </summary>
    public enum ParticipantStatus
    {
        /// <summary>招待中</summary>
        Invited = 0,

        /// <summary>出席予定</summary>
        Accepted = 1,

        /// <summary>出席予定なし</summary>
        Declined = 2,

        /// <summary>未定</summary>
        Tentative = 3
    }
}
