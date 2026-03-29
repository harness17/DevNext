using ClosedXML.Excel;
using Dev.CommonLibrary.Extensions;
using Microsoft.AspNetCore.Identity;
using Site.Common;
using Dev.CommonLibrary.Entity;
using Site.Entity;
using Site.Models;
using Site.Repository;
using System.Text;

namespace Site.Service
{
    /// <summary>
    /// CSV・Excel エクスポートサービス。
    /// 現在の検索条件に一致する全レコードをファイルに変換して返す。
    /// ポイント: ページングを行わず全件取得する点が一覧取得と異なる。
    /// </summary>
    public class ExportService
    {
        private readonly ApprovalRequestRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExportService(DBContext context, UserManager<ApplicationUser> userManager)
        {
            // ポイント: Repository はサービス内で new して使う（DI せず）
            _repo = new ApprovalRequestRepository(context);
            _userManager = userManager;
        }

        // ─── CSV エクスポート ──────────────────────────────────────────────────

        /// <summary>
        /// 現在の検索条件に一致するデータを CSV 形式（BOM 付き UTF-8）で返す。
        /// ポイント: BOM 付き UTF-8 にすることで Excel で開いても文字化けしない。
        /// </summary>
        public async Task<byte[]> ExportCsvAsync(ApprovalRequestCondViewModel? condVm, string currentUserId, bool isAdmin)
        {
            var rows = await GetExportRowsAsync(condVm, currentUserId, isAdmin);

            var sb = new StringBuilder();
            sb.AppendLine("申請ID,タイトル,申請者,状態,申請日時,承認・却下日時,承認者コメント");

            foreach (var (entity, requesterName) in rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    entity.Id.ToString(),
                    CsvEscape(entity.Title),
                    CsvEscape(requesterName),
                    CsvEscape(entity.Status.DisplayName() ?? entity.Status.ToString()),
                    entity.RequestedDate?.ToString("yyyy/MM/dd HH:mm") ?? "",
                    entity.ApprovedDate?.ToString("yyyy/MM/dd HH:mm") ?? "",
                    CsvEscape(entity.ApproverComment ?? ""),
                }));
            }

            // BOM + UTF-8 バイト列を結合して返す
            return Encoding.UTF8.GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();
        }

        // ─── Excel エクスポート ─────────────────────────────────────────────────

        /// <summary>
        /// 現在の検索条件に一致するデータを Excel 形式（.xlsx）で返す。
        /// ClosedXML を使用してヘッダースタイル・列幅自動調整を適用する。
        /// </summary>
        public async Task<byte[]> ExportExcelAsync(ApprovalRequestCondViewModel? condVm, string currentUserId, bool isAdmin)
        {
            var rows = await GetExportRowsAsync(condVm, currentUserId, isAdmin);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("承認申請一覧");

            // ─── ヘッダー行 ─────────────────────────────────────────────────────
            string[] headers = { "申請ID", "タイトル", "申請者", "状態", "申請日時", "承認・却下日時", "承認者コメント" };
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                // ポイント: ヘッダーは太字 + 背景色で見やすくする
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // ─── データ行 ────────────────────────────────────────────────────────
            for (int i = 0; i < rows.Count; i++)
            {
                var (entity, requesterName) = rows[i];
                int row = i + 2; // 1行目はヘッダーのため2行目から開始

                ws.Cell(row, 1).Value = entity.Id;
                ws.Cell(row, 2).Value = entity.Title;
                ws.Cell(row, 3).Value = requesterName;
                ws.Cell(row, 4).Value = entity.Status.DisplayName() ?? entity.Status.ToString();
                ws.Cell(row, 5).Value = entity.RequestedDate?.ToString("yyyy/MM/dd HH:mm") ?? "";
                ws.Cell(row, 6).Value = entity.ApprovedDate?.ToString("yyyy/MM/dd HH:mm") ?? "";
                ws.Cell(row, 7).Value = entity.ApproverComment ?? "";
            }

            // ポイント: AdjustToContents() で列幅をデータに合わせて自動調整する
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ─── 内部ユーティリティ ───────────────────────────────────────────────

        /// <summary>
        /// エクスポート用にページングなしで全件取得し、申請者名を解決して返す。
        /// </summary>
        private async Task<List<(ApprovalRequestEntity Entity, string RequesterName)>> GetExportRowsAsync(
            ApprovalRequestCondViewModel? condVm, string currentUserId, bool isAdmin)
        {
            // ポイント: エクスポートは全件対象のためページングモデルを組まず条件のみ渡す
            var cond = BuildCondModel(condVm, isAdmin ? null : currentUserId);
            var entities = _repo.GetBaseQuery(cond).OrderBy(x => x.Id).ToList();

            // ポイント: 申請者 ID を一括収集し、ループ内の N+1 を避けるためまとめて解決する
            var userIds = entities.Select(e => e.RequesterUserId).Distinct().ToList();
            var userMap = new Dictionary<string, string>();
            foreach (var uid in userIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
                userMap[uid] = user?.UserName ?? uid;
            }

            return entities
                .Select(e => (e, userMap.GetValueOrDefault(e.RequesterUserId, e.RequesterUserId)))
                .ToList();
        }

        /// <summary>
        /// ViewModel から Repository 用の検索条件モデルを組み立てる。
        /// ページング不要なため Pager は設定しない。
        /// </summary>
        private static ApprovalRequestCondModel BuildCondModel(ApprovalRequestCondViewModel? vm, string? requesterUserId)
        {
            var cond = new ApprovalRequestCondModel
            {
                Title = vm?.Title,
                RequesterUserId = requesterUserId,
            };

            if (vm != null && !string.IsNullOrEmpty(vm.Status) && int.TryParse(vm.Status, out int statusVal))
                cond.Status = (ApprovalStatus)statusVal;

            return cond;
        }

        /// <summary>
        /// CSV 用のエスケープ処理。カンマ・ダブルクォート・改行を含む場合はクォートで囲む。
        /// </summary>
        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
