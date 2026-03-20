using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dev.CommonLibrary.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Site.Common;
using Site.Entity;
using Site.Models;
using Site.Repository;
using System.Security.Claims;

namespace Site.Service
{
    public class DatabaseSampleService
    {
        private readonly DBContext _context;
        private readonly SampleEntityRepository _sampleRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DatabaseSampleService(DBContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            // ポイント: Repository はサービス内で new して使う（DIせず）
            //           Repository 自体が DBContext に依存するため、DI 済みの context を渡して初期化する
            _sampleRepository = new SampleEntityRepository(context);
            _httpContextAccessor = httpContextAccessor;
        }

        public DatabaseSampleViewModel GetSampleEntityList(DatabaseSampleViewModel model)
        {
            if (model.Cond == null) model.Cond = new DatabaseSampleCondViewModel();
            // ページャー設定（件数・ページ番号・ソート列 を CondViewModel に反映）
            LocalUtil.SetPager(model.Cond, model);
            // GetCondModel: View用CondViewModel → Repository用CondModel へAutoMapperで変換
            model.RowData = _sampleRepository.GetSampleEntityList(_sampleRepository.GetCondModel(model.Cond));
            return model;
        }

        // ポイント: AutoMapper の設定は都度 new MapperConfiguration() で行うローカル設定パターン
        //           グローバル設定（Profile）の代わりに、メソッド内で用途に特化したマッピングを定義できる
        //           NullLoggerFactory.Instance を渡すことで AutoMapper の内部警告ログを抑制する
        public void InsSampleEntity(DatabaseSampleDetailViewModel model, string? userName)
        {
            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleDetailViewModel, SampleEntity>()
                   // ポイント: ForMember で特定プロパティのマッピングを個別指定する
                   //           ここではログインユーザー名を ApplicationUserId に割り当てる
                   .ForMember(d => d.ApplicationUserId, o => o.MapFrom(s => userName)),
                NullLoggerFactory.Instance).CreateMapper();

            SampleEntity entity = mapper.Map<SampleEntity>(model);
            // ポイント: SetForCreate() は EntityBase の拡張メソッド
            //           CreateDate / UpdateDate / DelFlag などの共通フィールドを初期値セットする
            entity.SetForCreate();
            _sampleRepository.Insert(entity);
        }

        public DatabaseSampleDetailViewModel? GetSampleEntity(int id)
        {
            // ポイント: ProjectTo はプロパティ初期化子（= new List<>() 等）を実行しないため
            //           SelectById で Entity を取得してから Map する方が安全
            var entity = _sampleRepository.SelectById((long)id);
            if (entity == null) return null;

            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<SampleEntity, DatabaseSampleDetailViewModel>(),
                NullLoggerFactory.Instance).CreateMapper();
            return mapper.Map<DatabaseSampleDetailViewModel>(entity);
        }

        public void UpdSampleEntity(DatabaseSampleDetailViewModel model, IWebHostEnvironment env)
        {
            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleDetailViewModel, SampleEntity>(),
                NullLoggerFactory.Instance).CreateMapper();

            // ポイント: DB から既存エンティティを取得してから Map することで
            //           ViewModel にないフィールド（CreateDate 等）を上書きせずに済む
            SampleEntity updEntity = _sampleRepository.SelectById(model.Id!)!;
            updEntity = mapper.Map(model, updEntity);

            var attachFilesName = new List<string>();
            if (model.FileData_file != null && model.FileData_file.Any(f => f != null))
            {
                // ポイント: wwwroot/Uploads/Sample/{id}/ にIDごとのフォルダを作成してファイルを保存
                //           Directory.CreateDirectory は既存でもエラーにならない（冪等）
                var tempFolderPath = Path.Combine(env.WebRootPath, "Uploads", "Sample", model.Id?.ToString() ?? "0");
                Directory.CreateDirectory(tempFolderPath);

                foreach (var fileitem in model.FileData_file.Where(f => f != null))
                {
                    if (fileitem != null && Util.IsSafePath(fileitem.FileName, true))
                    {
                        // ポイント: Util.SetFileName でファイル名をサニタイズ（重複回避のタイムスタンプ付与等）
                        //           Util.IsSafePath でパストラバーサル攻撃を防ぐ
                        var fileName = Util.SetFileName(fileitem.FileName);
                        var filePath = Path.Combine(tempFolderPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            fileitem.CopyTo(stream);
                        attachFilesName.Add(fileName);
                    }
                }
            }
            // ポイント: 複数ファイル名をカンマ区切りで1カラムに保存するシンプルな設計
            updEntity.FileData = string.Join(",", attachFilesName);
            _sampleRepository.Update(updEntity);
        }

        public void DelSampleEntity(int id)
        {
            var entity = _context.SampleEntity.Find((long)id);
            // ポイント: LogicalDelete で DelFlag を true にする論理削除
            //           データを物理的に削除しないため履歴追跡・復元が可能
            if (entity != null) _sampleRepository.LogicalDelete(entity);
        }

        public void InsertFile(ref DatabaseSampleImportViewModel model)
        {
            if (model.ImportDataFile != null)
            {
                // ポイント: IFormFile.OpenReadStream() でファイルの中身を Stream として取得
                //           一時ファイルへの保存なしに直接処理できる
                model.ImportErrList = ImportXlsxSample(model.ImportDataFile.OpenReadStream());
            }
        }

        // ポイント: EPPlus を使用した Excel ファイルのインポート処理
        //           LicenseContext.NonCommercial は非商用利用の際に必要な設定（商用利用は Commercial を指定）
        private List<string> ImportXlsxSample(Stream fileStream)
        {
            var returnlist = new List<string>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var excelPackage = new ExcelPackage(fileStream))
            {
                var worksheet = excelPackage.Workbook.Worksheets.First();
                // Dimension が null = シートが空の場合は早期リターン
                if (worksheet.Dimension == null) return returnlist;

                var rowCount = worksheet.Dimension.Rows;
                var entityList = new List<SampleEntity>();

                // ポイント: 1行目はヘッダー行と想定し2行目からループ
                for (int row = 2; row <= rowCount; row++)
                {
                    int col = 3;
                    var tempEntity = new SampleEntity();
                    tempEntity.StringData = worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
                    col++;
                    if (worksheet.Cells[row, col].Value != null)
                        int.TryParse(worksheet.Cells[row, col].Value.ToString(), out int intData);
                    col++;
                    if (worksheet.Cells[row, col].Value != null)
                        bool.TryParse(worksheet.Cells[row, col].Value.ToString(), out bool boolData);
                    col++;
                    // ポイント: Enum.TryParse で文字列から Enum 値に変換（変換失敗時はデフォルト値になる）
                    Enum.TryParse(worksheet.Cells[row, col].Value as string, out SampleEnum sampleEnum);
                    tempEntity.EnumData = sampleEnum;
                    col++;
                    Enum.TryParse(worksheet.Cells[row, col].Value as string, out SampleEnum2 sampleEnum2);
                    tempEntity.EnumData2 = sampleEnum2;
                    tempEntity.SetForCreate();
                    entityList.Add(tempEntity);
                }

                // エラーがなければ一括 INSERT（バッチ処理でDBへの往復回数を削減）
                if (returnlist.Count == 0)
                    _sampleRepository.BatchInsert(entityList);
            }

            return returnlist;
        }

        // ポイント: EPPlus を使用した Excel エクスポート
        //           テンプレートファイルが存在する場合は読み込んで使用（書式・ヘッダー等をテンプレートで管理できる）
        //           MemoryStream でメモリ上に出力してコントローラーへ返す（ディスク保存なし）
        public MemoryStream ExportFile(DatabaseSampleViewModel model, IWebHostEnvironment env)
        {
            var rowData = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var templatePath = Path.Combine(env.WebRootPath, "Templates", "Excel", "SampleEntityTemplate.xlsx");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var memorystream = new MemoryStream();

            ExcelPackage pck;
            // ポイント: テンプレートファイルがあれば読み込む、なければ空の ExcelPackage を生成
            if (File.Exists(templatePath))
                pck = new ExcelPackage(new FileInfo(templatePath));
            else
                pck = new ExcelPackage();

            using (pck)
            {
                // FirstOrDefault でシートが存在しない場合は新規作成
                var ws = pck.Workbook.Worksheets.FirstOrDefault() ?? pck.Workbook.Worksheets.Add("Sheet1");
                int intBaseRow = 2;  // 2行目からデータ書き込み（1行目はテンプレートのヘッダー行）
                foreach (var row in rowData)
                {
                    int intCol = 1;
                    ws.Cells[intBaseRow, intCol++].Value = row.Id;
                    ws.Cells[intBaseRow, intCol++].Value = row.ApplicationUserId;
                    ws.Cells[intBaseRow, intCol++].Value = row.StringData;
                    ws.Cells[intBaseRow, intCol++].Value = row.IntData;
                    ws.Cells[intBaseRow, intCol++].Value = row.BoolData;
                    ws.Cells[intBaseRow, intCol++].Value = row.EnumData.ToString();
                    ws.Cells[intBaseRow, intCol++].Value = row.EnumData2.ToString();
                    intBaseRow++;
                }
                pck.SaveAs(memorystream);
                // ポイント: SaveAs 後は Position が末尾にあるため 0 にリセットしないと読み取れない
                memorystream.Position = 0;
            }

            return memorystream;
        }

        /// <summary>
        /// 検索結果をPDFとして出力する
        /// </summary>
        /// <param name="model">検索条件を含むビューモデル</param>
        /// <returns>PDF のメモリストリーム</returns>
        // ポイント: QuestPDF を使用したPDF出力
        //           Document.Create → Page → Header/Content/Footer の構造で組み立てる
        //           Fluent API（メソッドチェーン）でレイアウトを定義する
        public MemoryStream ExportPdf(DatabaseSampleViewModel model)
        {
            // QuestPDF のライセンス設定（Community = 無償利用）
            QuestPDF.Settings.License = LicenseType.Community;

            var rowData = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());  // 横向きA4
                    page.Margin(1, Unit.Centimetre);
                    // ポイント: 日本語対応フォントを複数指定しフォールバックさせる
                    //           Noto Sans CJK JP が存在しない場合は Meiryo、次に MS Gothic を使用
                    page.DefaultTextStyle(x => x.FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic").FontSize(9));

                    // ヘッダー
                    page.Header()
                        .PaddingBottom(5)
                        .Text($"DBサンプル一覧　出力日時：{DateTime.Now:yyyy/MM/dd HH:mm}")
                        .FontSize(11).Bold();

                    // テーブル本体
                    page.Content().Table(table =>
                    {
                        // 列幅定義
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);  // ID
                            columns.RelativeColumn(3);   // 文字列データ
                            columns.ConstantColumn(70);  // 数値データ
                            columns.ConstantColumn(60);  // BoolData
                            columns.ConstantColumn(80);  // EnumData
                            columns.ConstantColumn(80);  // EnumData2
                            columns.RelativeColumn(2);   // 作成日時
                        });

                        // テーブルヘッダー行
                        // ポイント: ローカル関数でセルスタイルを共通化するパターン
                        static IContainer HeaderCellStyle(IContainer container) =>
                            container.DefaultTextStyle(x => x.Bold())
                                     .BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                     .Background(Colors.Grey.Lighten3)
                                     .PaddingVertical(4).PaddingHorizontal(4);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("ID");
                            header.Cell().Element(HeaderCellStyle).Text("文字列データ");
                            header.Cell().Element(HeaderCellStyle).Text("数値データ");
                            header.Cell().Element(HeaderCellStyle).Text("BoolData");
                            header.Cell().Element(HeaderCellStyle).Text("EnumData");
                            header.Cell().Element(HeaderCellStyle).Text("EnumData2");
                            header.Cell().Element(HeaderCellStyle).Text("作成日時");
                        });

                        // データ行
                        // ポイント: Select((r, i) => (r, i)) でインデックスを同時に取得するパターン
                        foreach (var (row, index) in rowData.Select((r, i) => (r, i)))
                        {
                            // 偶数行に背景色を付けて可読性を向上
                            var background = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                            static IContainer DataCellStyle(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(3).PaddingHorizontal(4);

                            IContainer CellWithBg(IContainer c) =>
                                DataCellStyle(c).Background(background);

                            table.Cell().Element(CellWithBg).Text(row.Id.ToString());
                            table.Cell().Element(CellWithBg).Text(row.StringData);
                            table.Cell().Element(CellWithBg).AlignRight().Text(row.IntData.ToString());
                            table.Cell().Element(CellWithBg).AlignCenter().Text(row.BoolData ? "あり" : "なし");
                            table.Cell().Element(CellWithBg).Text(row.EnumData.ToString());
                            table.Cell().Element(CellWithBg).Text(row.EnumData2.ToString());
                            table.Cell().Element(CellWithBg).Text(row.CreateDate.ToString("yyyy/MM/dd HH:mm"));
                        }
                    });

                    // フッター（ページ番号）
                    page.Footer()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            }).GeneratePdf(memoryStream);

            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// 単体データをPDFとして出力する。
        /// 添付ファイルが png / jpeg / jpg の場合は画像をPDF内に直接埋め込む。
        /// </summary>
        /// <param name="id">出力対象のID</param>
        /// <param name="env">ファイルパス解決に使用するホスト環境</param>
        /// <returns>PDFのメモリストリーム。対象データが存在しない場合はnull</returns>
        public MemoryStream? ExportPdfSingle(int id, IWebHostEnvironment env)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var entity = _sampleRepository.SelectById((long)id);
            if (entity == null) return null;

            // 添付ファイルを画像と非画像に分類する
            // ポイント: HashSet<string> + StringComparer.OrdinalIgnoreCase で拡張子を大文字小文字無視で比較
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpeg", ".jpg" };
            var uploadFolder = Path.Combine(env.WebRootPath, "Uploads", "Sample", entity.Id.ToString());
            // ポイント: カンマ区切りのファイル名文字列を Split で配列に変換
            //           RemoveEmptyEntries / TrimEntries で空要素・前後スペースを除去する
            var fileNames = (entity.FileData ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic").FontSize(10));

                    // ヘッダー
                    page.Header()
                        .PaddingBottom(10)
                        .Column(col =>
                        {
                            col.Item().Text("DBサンプル 詳細").FontSize(14).Bold();
                            col.Item().Text($"出力日時：{DateTime.Now:yyyy/MM/dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                    page.Content().Column(content =>
                    {
                        // 詳細テーブル
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120); // 項目名
                                columns.RelativeColumn();    // 値
                            });

                            static IContainer LabelStyle(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3)
                                 .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(6).PaddingHorizontal(8)
                                 .DefaultTextStyle(x => x.Bold());

                            static IContainer ValueStyle(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(6).PaddingHorizontal(8);

                            // ポイント: ローカル関数 Row() で繰り返しパターンをまとめる
                            void Row(string label, string value)
                            {
                                table.Cell().Element(LabelStyle).Text(label);
                                table.Cell().Element(ValueStyle).Text(value);
                            }

                            Row("ID",          entity.Id.ToString());
                            Row("ユーザーID",  entity.ApplicationUserId ?? "");
                            Row("文字列データ", entity.StringData);
                            Row("数値データ",  entity.IntData.ToString());
                            Row("BoolData",    entity.BoolData ? "あり" : "なし");
                            Row("EnumData",    entity.EnumData.ToString());
                            Row("EnumData2",   entity.EnumData2.ToString());
                            Row("作成日時",    entity.CreateDate.ToString("yyyy/MM/dd HH:mm:ss"));
                            Row("更新日時",    entity.UpdateDate.ToString("yyyy/MM/dd HH:mm:ss"));

                            // 非画像ファイルのみファイル名行として表示
                            var nonImageFiles = fileNames
                                .Where(f => !imageExtensions.Contains(Path.GetExtension(f)))
                                .ToList();
                            if (nonImageFiles.Any())
                                Row("添付ファイル", string.Join("\n", nonImageFiles));
                        });

                        // ポイント: 画像ファイルのみ PDF 内に直接埋め込む
                        //           File.ReadAllBytes でバイト配列として読み込み Image() に渡す
                        //           FitArea() でコンテナに収まるようにリサイズ（アスペクト比維持）
                        foreach (var fileName in fileNames.Where(f => imageExtensions.Contains(Path.GetExtension(f))))
                        {
                            var filePath = Path.Combine(uploadFolder, fileName);
                            if (!File.Exists(filePath)) continue;

                            var imageBytes = File.ReadAllBytes(filePath);

                            content.Item().PaddingTop(12).Column(imgCol =>
                            {
                                // ファイル名キャプション
                                imgCol.Item()
                                    .Text(fileName)
                                    .FontSize(9).FontColor(Colors.Grey.Medium);

                                // 画像本体（縦横300pt以内に収めアスペクト比を維持）
                                imgCol.Item()
                                    .MaxHeight(300)
                                    .Image(imageBytes)
                                    .FitArea();
                            });
                        }
                    });
                });
            }).GeneratePdf(memoryStream);

            memoryStream.Position = 0;
            return memoryStream;
        }

        // ポイント: AutoMapper の前付け名 (RecognizeDestinationPrefixes) と
        //           後付け名 (RecognizeDestinationPostfixes) によるプロパティ名マッピングのサンプル
        //           例: SampleEntity.StringData → DatabaseMapperUsageSampleDataModel.MapperUsageStringData
        //               (Prefix "MapperUsage" を認識して StringData にマッピング)
        public DatabaseMapperUsageSampleViewModel GetMapperUsage()
        {
            var model = new DatabaseMapperUsageSampleViewModel();
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                // ポイント: RecognizeDestinationPrefixes で宛先プロパティ名の前付けを無視してマッピング
                cfg.RecognizeDestinationPrefixes("MapperUsage");
                // ポイント: RecognizeDestinationPostfixes で宛先プロパティ名の後付けを無視してマッピング
                cfg.RecognizeDestinationPostfixes("WithMapper");
                cfg.CreateMap<SampleEntity, DatabaseMapperUsageSampleDataModel>()
                   // 名前が異なるプロパティは ForMember で明示的にマッピングする
                   .ForMember(dest => dest.LastUpdatedBy, opt => opt.MapFrom(src => src.UpdateApplicationUserId))
                   .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdateDate));
            }, NullLoggerFactory.Instance);

            // ポイント: GetQueryAs<T> は IQueryable に ProjectTo を適用してSQL側でSELECT射影する
            //           必要なカラムだけ取得できるためパフォーマンスが向上する（SELECT * にならない）
            model.Data = _sampleRepository
                .GetQueryAs<DatabaseMapperUsageSampleDataModel>(config: mapperConfig)
                .ToList();
            return model;
        }
    }
}
