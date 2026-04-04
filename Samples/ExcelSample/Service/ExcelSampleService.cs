using ClosedXML.Excel;
using ExcelSample.Data;
using ExcelSample.Entity;
using ExcelSample.Models;
using ExcelSample.Repository;

namespace ExcelSample.Service
{
    /// <summary>
    /// ExcelSample ビジネスロジック
    /// ポイント: Excel エクスポート・インポートの実装パターンを示すサービス
    /// </summary>
    public class ExcelSampleService
    {
        private readonly ExcelSampleDbContext _context;
        private readonly ExcelItemRepository _repository;

        public ExcelSampleService(ExcelSampleDbContext context, ExcelItemRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        // ─────────────────────────────────────────────
        // 一覧取得
        // ─────────────────────────────────────────────

        /// <summary>商品一覧を取得して ViewModel に設定する</summary>
        public ExcelSampleViewModel GetItemList(ExcelSampleViewModel model)
        {
            var cond = new ExcelItemCondModel();

            // ポイント: Id 降順をデフォルトソートとする
            IQueryable<ExcelItemEntity> query = _repository.GetBaseQuery(cond)
                .OrderByDescending(x => x.Id);

            model.Rows = query.ToList();
            return model;
        }

        // ─────────────────────────────────────────────
        // 削除
        // ─────────────────────────────────────────────

        /// <summary>指定 Id の商品を論理削除する</summary>
        public void DeleteItem(long id, string? userName)
        {
            var entity = _repository.SelectById(id);
            if (entity == null) return;

            _repository.LogicalDelete(entity);
        }

        // ─────────────────────────────────────────────
        // エクスポート（Excel）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 商品一覧を Excel ファイルとして出力する
        /// ポイント: ClosedXML でヘッダー行・スタイリング・列幅自動調整を実装するパターン
        /// </summary>
        public MemoryStream ExportExcel()
        {
            var rows = _repository.GetBaseQuery().OrderByDescending(x => x.Id).ToList();
            var memoryStream = new MemoryStream();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("商品一覧");

                // ── ヘッダー行 ──────────────────────────
                var headers = new[] { "ID", "商品名", "カテゴリ", "単価（円）", "在庫数", "登録日時" };
                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(1, col);
                    cell.Value = headers[col - 1];

                    // ポイント: ヘッダーを太字・背景色付きにしてデータ行と区別する
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // ── データ行 ──────────────────────────
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    int excelRow = i + 2;  // 1行目はヘッダーなので2行目から

                    ws.Cell(excelRow, 1).Value = row.Id;
                    ws.Cell(excelRow, 2).Value = row.Name;
                    ws.Cell(excelRow, 3).Value = row.Category;
                    ws.Cell(excelRow, 4).Value = row.Price;
                    ws.Cell(excelRow, 5).Value = row.Quantity;
                    ws.Cell(excelRow, 6).Value = row.CreateDate.ToString("yyyy/MM/dd HH:mm");

                    // ポイント: 1行おきに背景色を設定して視認性を上げる（ゼブラストライプ）
                    if (i % 2 == 1)
                    {
                        ws.Row(excelRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
                    }

                    // 数値列を右揃えにする
                    ws.Cell(excelRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    ws.Cell(excelRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // 全セルに罫線を引く
                    for (int col = 1; col <= headers.Length; col++)
                        ws.Cell(excelRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // ポイント: 列幅をコンテンツに合わせて自動調整する
                ws.Columns().AdjustToContents();

                workbook.SaveAs(memoryStream);
                memoryStream.Position = 0;
            }

            return memoryStream;
        }

        // ─────────────────────────────────────────────
        // インポート（Excel）
        // ─────────────────────────────────────────────

        /// <summary>
        /// Excel ファイルをインポートして商品データをDBに登録する
        /// ポイント: 行ごとにバリデーションを行い、エラー行の行番号をエラーリストに返すパターン
        /// </summary>
        public (int successCount, List<string> errors) ImportExcel(Stream fileStream, string? userName)
        {
            var errors = new List<string>();
            var entities = new List<ExcelItemEntity>();

            using (var workbook = new XLWorkbook(fileStream))
            {
                var ws = workbook.Worksheets.First();

                // ポイント: 1行目はヘッダーとしてスキップし、2行目からデータとして読み込む
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                for (int rowNum = 2; rowNum <= lastRow; rowNum++)
                {
                    var row = ws.Row(rowNum);

                    // ポイント: 空行（すべての対象セルが空）はスキップする
                    if (row.Cell(1).IsEmpty() && row.Cell(2).IsEmpty() && row.Cell(3).IsEmpty())
                        continue;

                    // 各セルの値を取得
                    var name     = row.Cell(1).GetString().Trim();
                    var category = row.Cell(2).GetString().Trim();
                    var priceStr = row.Cell(3).GetString().Trim();
                    var qtyStr   = row.Cell(4).GetString().Trim();

                    // ── バリデーション ──────────────────
                    if (string.IsNullOrEmpty(name))
                    {
                        errors.Add($"{rowNum}行目: 商品名は必須です。");
                        continue;
                    }
                    if (name.Length > 100)
                    {
                        errors.Add($"{rowNum}行目: 商品名は100文字以内で入力してください。");
                        continue;
                    }
                    if (string.IsNullOrEmpty(category))
                    {
                        errors.Add($"{rowNum}行目: カテゴリは必須です。");
                        continue;
                    }
                    if (!int.TryParse(priceStr, out int price) || price < 0)
                    {
                        errors.Add($"{rowNum}行目: 単価に無効な値が入力されています（「{priceStr}」）。");
                        continue;
                    }
                    if (!int.TryParse(qtyStr, out int quantity) || quantity < 0)
                    {
                        errors.Add($"{rowNum}行目: 在庫数に無効な値が入力されています（「{qtyStr}」）。");
                        continue;
                    }

                    // ── エンティティ生成 ──────────────
                    var entity = new ExcelItemEntity
                    {
                        Name     = name,
                        Category = category,
                        Price    = price,
                        Quantity = quantity,
                    };
                    // ポイント: SetForCreate() で共通監査カラム（作成者・作成日時）を初期化する
                    entity.SetForCreate();
                    entities.Add(entity);
                }
            }

            // エラーがなかった行だけ一括 Insert する
            if (entities.Count > 0)
            {
                _context.ExcelItemEntity.AddRange(entities);
                _context.SaveChanges();
            }

            return (entities.Count, errors);
        }
    }
}
