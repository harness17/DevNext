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
            _sampleRepository = new SampleEntityRepository(context);
            _httpContextAccessor = httpContextAccessor;
        }

        public DatabaseSampleViewModel GetSampleEntityList(DatabaseSampleViewModel model)
        {
            if (model.Cond == null) model.Cond = new DatabaseSampleCondViewModel();
            localutil.SetPager(model.Cond, model);
            model.RowData = _sampleRepository.GetSampleEntityList(_sampleRepository.GetCondModel(model.Cond));
            return model;
        }

        public void InsSampleEntity(DatabaseSampleDetailViewModel model, string? userName)
        {
            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleDetailViewModel, SampleEntity>()
                   .ForMember(d => d.ApplicationUserId, o => o.MapFrom(s => userName)),
                NullLoggerFactory.Instance).CreateMapper();

            SampleEntity entity = mapper.Map<SampleEntity>(model);
            entity.SetForCreate();
            _sampleRepository.Insert(entity);
        }

        public DatabaseSampleDetailViewModel? GetSampleEntity(int id)
        {
            // ProjectTo はプロパティ初期化子を実行しないため、SelectById → Map で取得する
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

            SampleEntity updEntity = _sampleRepository.SelectById(model.Id!)!;
            updEntity = mapper.Map(model, updEntity);

            var attachFilesName = new List<string>();
            if (model.FileData_file != null && model.FileData_file.Any(f => f != null))
            {
                var tempFolderPath = Path.Combine(env.WebRootPath, "Uploads", "Sample", model.Id?.ToString() ?? "0");
                Directory.CreateDirectory(tempFolderPath);

                foreach (var fileitem in model.FileData_file.Where(f => f != null))
                {
                    if (fileitem != null && util.IsSafePath(fileitem.FileName, true))
                    {
                        var fileName = util.SetFileName(fileitem.FileName);
                        var filePath = Path.Combine(tempFolderPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            fileitem.CopyTo(stream);
                        attachFilesName.Add(fileName);
                    }
                }
            }
            updEntity.FileData = string.Join(",", attachFilesName);
            _sampleRepository.Update(updEntity);
        }

        public void DelSampleEntity(int id)
        {
            var entity = _context.SampleEntity.Find((long)id);
            if (entity != null) _sampleRepository.LogicalDelete(entity);
        }

        public void InsertFile(ref DatabaseSampleImportViewModel model)
        {
            if (model.ImportDataFile != null)
            {
                model.ImportErrList = ImportXlsxSample(model.ImportDataFile.OpenReadStream());
            }
        }

        private List<string> ImportXlsxSample(Stream fileStream)
        {
            var returnlist = new List<string>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var excelPackage = new ExcelPackage(fileStream))
            {
                var worksheet = excelPackage.Workbook.Worksheets.First();
                if (worksheet.Dimension == null) return returnlist;

                var rowCount = worksheet.Dimension.Rows;
                var entityList = new List<SampleEntity>();

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
                    Enum.TryParse(worksheet.Cells[row, col].Value as string, out SampleEnum sampleEnum);
                    tempEntity.EnumData = sampleEnum;
                    col++;
                    Enum.TryParse(worksheet.Cells[row, col].Value as string, out SampleEnum2 sampleEnum2);
                    tempEntity.EnumData2 = sampleEnum2;
                    tempEntity.SetForCreate();
                    entityList.Add(tempEntity);
                }

                if (returnlist.Count == 0)
                    _sampleRepository.BatchInsert(entityList);
            }

            return returnlist;
        }

        public MemoryStream ExportFile(DatabaseSampleViewModel model, IWebHostEnvironment env)
        {
            var rowData = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var templatePath = Path.Combine(env.WebRootPath, "Templates", "Excel", "SampleEntityTemplate.xlsx");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var memorystream = new MemoryStream();

            ExcelPackage pck;
            if (File.Exists(templatePath))
                pck = new ExcelPackage(new FileInfo(templatePath));
            else
                pck = new ExcelPackage();

            using (pck)
            {
                var ws = pck.Workbook.Worksheets.FirstOrDefault() ?? pck.Workbook.Worksheets.Add("Sheet1");
                int intBaseRow = 2;
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
                memorystream.Position = 0;
            }

            return memorystream;
        }

        /// <summary>
        /// 検索結果をPDFとして出力する
        /// </summary>
        /// <param name="model">検索条件を含むビューモデル</param>
        /// <returns>PDF のメモリストリーム</returns>
        public MemoryStream ExportPdf(DatabaseSampleViewModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var rowData = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
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
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpeg", ".jpg" };
            var uploadFolder = Path.Combine(env.WebRootPath, "Uploads", "Sample", entity.Id.ToString());
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

                        // 画像ファイルをテーブルの下に埋め込む
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

        public DatabaseMapperUsageSampleViewModel GetMapperUsage()
        {
            var model = new DatabaseMapperUsageSampleViewModel();
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.RecognizeDestinationPrefixes("MapperUsage");
                cfg.RecognizeDestinationPostfixes("WithMapper");
                cfg.CreateMap<SampleEntity, DatabaseMapperUsageSampleDataModel>()
                   .ForMember(dest => dest.LastUpdatedBy, opt => opt.MapFrom(src => src.UpdateApplicationUserId))
                   .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdateDate));
            }, NullLoggerFactory.Instance);

            model.Data = _sampleRepository
                .GetQueryAs<DatabaseMapperUsageSampleDataModel>(config: mapperConfig)
                .ToList();
            return model;
        }
    }
}
