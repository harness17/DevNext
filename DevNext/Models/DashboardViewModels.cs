namespace Site.Models
{
    /// <summary>
    /// グラフの1項目（ラベルと件数のペア）
    /// Chart.js の labels[] / data[] に展開する汎用 DTO
    /// </summary>
    public class ChartItemDto
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    /// <summary>
    /// 日別件数データ（折れ線グラフ用）
    /// </summary>
    public class DailyCountDto
    {
        /// <summary>日付文字列（MM/dd 形式）</summary>
        public string Date { get; set; } = "";
        public int Count { get; set; }
    }

    /// <summary>
    /// メール送信 成功/失敗 件数
    /// </summary>
    public class MailSuccessRatioDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    /// <summary>
    /// サマリーカード用 DTO（各エンティティの総件数）
    /// </summary>
    public class DashboardSummaryDto
    {
        public int SampleCount { get; set; }
        public int MailLogCount { get; set; }
        public int FileCount { get; set; }
        public int WizardCount { get; set; }
    }

    /// <summary>
    /// ダッシュボード全チャートデータ（Ajax API の返却型）
    /// ポーリング 1 リクエストで全グラフを更新できるよう一括で返す
    /// </summary>
    public class DashboardChartDataDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<ChartItemDto> SampleEnumDistribution { get; set; } = new();
        public MailSuccessRatioDto MailSuccessRatio { get; set; } = new();
        public List<DailyCountDto> MailDailyTrend { get; set; } = new();
        public List<ChartItemDto> FileTypeDistribution { get; set; } = new();
        public List<ChartItemDto> WizardCategoryDistribution { get; set; } = new();
    }

    /// <summary>
    /// ダッシュボード Index ビュー用 ViewModel
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>グラフデータ取得 API の URL（ビューにハードコードしないよう Controller から渡す）</summary>
        public string ChartDataUrl { get; set; } = "";
    }
}
