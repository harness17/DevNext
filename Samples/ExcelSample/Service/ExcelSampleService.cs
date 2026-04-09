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

        // ─────────────────────────────────────────────
        // エクスポート（CSV）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 商品一覧を CSV ファイルとして出力する
        /// ポイント: UTF-8 BOM 付きで出力することで Excel で開いたときに文字化けを防ぐ
        ///           フィールドにカンマ・ダブルクォート・改行が含まれる場合はダブルクォートで囲む（RFC 4180 準拠）
        /// </summary>
        public MemoryStream ExportCsv()
        {
            var rows = _repository.GetBaseQuery().OrderByDescending(x => x.Id).ToList();
            var memoryStream = new MemoryStream();

            // ポイント: new UTF8Encoding(true) の引数 true が BOM 付きを指定する
            using (var writer = new StreamWriter(memoryStream, new System.Text.UTF8Encoding(true), leaveOpen: true))
            {
                // ── ヘッダー行 ──────────────────────────
                writer.WriteLine("ID,商品名,カテゴリ,単価（円）,在庫数,登録日時");

                // ── データ行 ──────────────────────────
                foreach (var row in rows)
                {
                    writer.WriteLine(string.Join(",",
                        row.Id,
                        EscapeCsvField(row.Name),
                        EscapeCsvField(row.Category),
                        row.Price,
                        row.Quantity,
                        EscapeCsvField(row.CreateDate.ToString("yyyy/MM/dd HH:mm"))
                    ));
                }
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// CSV フィールドのエスケープ処理
        /// ポイント: カンマ・ダブルクォート・改行を含む場合はダブルクォートで囲み、
        ///           フィールド内のダブルクォートは "" にエスケープする（RFC 4180）
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }

        // ─────────────────────────────────────────────
        // インポート（CSV）
        // ─────────────────────────────────────────────

        /// <summary>
        /// CSV ファイルをインポートして商品データをDBに登録する
        /// ポイント: BOM 付き UTF-8 も自動認識する StreamReader を使用する
        ///           列順は「商品名 / カテゴリ / 単価（円） / 在庫数」（Excel インポートと同一）
        /// </summary>
        public (int successCount, List<string> errors) ImportCsv(Stream fileStream, string? userName)
        {
            var errors = new List<string>();
            var entities = new List<ExcelItemEntity>();

            // ポイント: detectEncodingFromByteOrderMarks: true で BOM 付き UTF-8 を自動検出する
            using (var reader = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true))
            {
                // 1行目はヘッダーとしてスキップ
                var headerLine = reader.ReadLine();
                if (headerLine == null) return (0, errors);

                int rowNum = 1; // ヘッダーが1行目なのでデータは2行目から
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    rowNum++;

                    // 空行はスキップ
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // ポイント: RFC 4180 対応の CSV パーサーで quoted フィールドを正しく分割する
                    var fields = ParseCsvLine(line);

                    if (fields.Count < 4)
                    {
                        errors.Add($"{rowNum}行目: 列数が不足しています（必要: 4列, 実際: {fields.Count}列）。");
                        continue;
                    }

                    var name     = fields[0].Trim();
                    var category = fields[1].Trim();
                    var priceStr = fields[2].Trim();
                    var qtyStr   = fields[3].Trim();

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
                    entity.SetForCreate();
                    entities.Add(entity);
                }
            }

            if (entities.Count > 0)
            {
                _context.ExcelItemEntity.AddRange(entities);
                _context.SaveChanges();
            }

            return (entities.Count, errors);
        }

        /// <summary>
        /// CSV 1行を RFC 4180 に従ってフィールドリストに分割する
        /// ポイント: ダブルクォートで囲まれたフィールド内のカンマ・改行・"" エスケープを正しく処理する
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            int i = 0;

            while (i <= line.Length)
            {
                if (i == line.Length)
                {
                    // 末尾のカンマ後の空フィールド
                    fields.Add("");
                    break;
                }

                if (line[i] == '"')
                {
                    // quoted フィールド
                    i++; // 開きクォートをスキップ
                    var sb = new System.Text.StringBuilder();

                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                // "" → " のエスケープ
                                sb.Append('"');
                                i += 2;
                            }
                            else
                            {
                                // 閉じクォート
                                i++;
                                break;
                            }
                        }
                        else
                        {
                            sb.Append(line[i]);
                            i++;
                        }
                    }

                    fields.Add(sb.ToString());
                    // 次のカンマをスキップ
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    // unquoted フィールド
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    fields.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++; // カンマをスキップ
                }
            }

            return fields;
        }
    }
}
