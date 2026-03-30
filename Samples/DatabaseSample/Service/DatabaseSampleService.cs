using AutoMapper;
using ClosedXML.Excel;
using DatabaseSample.Common;
using DatabaseSample.Data;
using DatabaseSample.Entity;
using DatabaseSample.Models;
using DatabaseSample.Repository;
using Dev.CommonLibrary.Common;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DatabaseSample.Service
{
    public class DatabaseSampleService
    {
        private readonly DatabaseSampleDbContext _context;
        private readonly SampleEntityRepository _sampleRepository;
        private readonly SampleEntityChildRepository _childRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DatabaseSampleService(DatabaseSampleDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            // ポイント: Repository はサービス内で new して使う（DIせず）
            //           Repository 自体が DBContext に依存するため、DI 済みの context を渡して初期化する
            _sampleRepository = new SampleEntityRepository(context);
            _childRepository  = new SampleEntityChildRepository(context);
            _httpContextAccessor = httpContextAccessor;
        }

        public DatabaseSampleViewModel GetSampleEntityList(DatabaseSampleViewModel model)
        {
            if (model.Cond == null) model.Cond = new DatabaseSampleCondViewModel();
            // ページャー設定（件数・ページ番号・ソート列を CondViewModel に反映）
            LocalUtil.SetPager(model.Cond, model);
            model.RowData = _sampleRepository.GetSampleEntityList(
                _sampleRepository.GetCondModel(model.Cond));
            return model;
        }

        // ポイント: AutoMapper の設定は都度 new MapperConfiguration() で行うローカル設定パターン
        //           グローバル設定（Profile）の代わりに、メソッド内で用途に特化したマッピングを定義できる
        public void InsSampleEntity(DatabaseSampleDetailViewModel model, string? userName)
        {
            var mapper = new MapperConfiguration(cfg =>
                cfg.CreateMap<DatabaseSampleDetailViewModel, SampleEntity>()
                   // ポイント: ForMember でログインユーザー名を ApplicationUserId に割り当てる
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
                var tempFolderPath = Path.Combine(env.WebRootPath, "Uploads", "Sample", model.Id?.ToString() ?? "0");
                Directory.CreateDirectory(tempFolderPath);

                foreach (var fileitem in model.FileData_file.Where(f => f != null))
                {
                    if (fileitem != null && Dev.CommonLibrary.Common.Util.IsSafePath(fileitem.FileName, true))
                    {
                        var fileName = Dev.CommonLibrary.Common.Util.SetFileName(fileitem.FileName);
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
            // ポイント: 子エンティティを先に論理削除してから親を論理削除する
            _childRepository.LogicalDeleteByParentId(id);

            var entity = _context.SampleEntity.Find((long)id);
            if (entity != null) _sampleRepository.LogicalDelete(entity);
        }

        // ─────────────────────────────────────────────
        // 子エンティティ CRUD
        // ─────────────────────────────────────────────

        /// <summary>親エンティティの詳細（親情報 + 子一覧）を取得して返す</summary>
        public DatabaseSampleDetailsViewModel? GetParentDetails(long id)
        {
            var parent = _sampleRepository.SelectById(id);
            if (parent == null || parent.DelFlag) return null;

            return new DatabaseSampleDetailsViewModel
            {
                Parent   = parent,
                Children = _childRepository.GetChildrenByParentId(id)
            };
        }

        /// <summary>子エンティティを編集 ViewModel に変換して返す</summary>
        public DatabaseSampleChildEditViewModel? GetChildEditModel(long id)
        {
            var entity = _childRepository.SelectById(id);
            if (entity == null || entity.DelFlag) return null;

            var mapper = new MapperConfiguration(
                cfg => cfg.CreateMap<SampleEntityChild, DatabaseSampleChildEditViewModel>()
                          // ポイント: SumpleEntityID（FK）を ViewModel の ParentId にマッピングする
                          .ForMember(d => d.ParentId, o => o.MapFrom(s => s.SumpleEntityID)),
                NullLoggerFactory.Instance).CreateMapper();

            return mapper.Map<DatabaseSampleChildEditViewModel>(entity);
        }

        /// <summary>子エンティティを新規作成する</summary>
        public void InsChild(DatabaseSampleChildEditViewModel model)
        {
            var mapper = new MapperConfiguration(
                cfg => cfg.CreateMap<DatabaseSampleChildEditViewModel, SampleEntityChild>()
                          .ForMember(d => d.SumpleEntityID, o => o.MapFrom(s => s.ParentId)),
                NullLoggerFactory.Instance).CreateMapper();

            var entity = mapper.Map<SampleEntityChild>(model);
            _childRepository.Insert(entity);
        }

        /// <summary>子エンティティを更新する</summary>
        public void UpdChild(DatabaseSampleChildEditViewModel model)
        {
            var entity = _childRepository.SelectById(model.Id!)!;

            var mapper = new MapperConfiguration(
                cfg => cfg.CreateMap<DatabaseSampleChildEditViewModel, SampleEntityChild>()
                          .ForMember(d => d.SumpleEntityID, o => o.MapFrom(s => s.ParentId)),
                NullLoggerFactory.Instance).CreateMapper();

            entity = mapper.Map(model, entity);
            _childRepository.Update(entity);
        }

        /// <summary>子エンティティを論理削除する</summary>
        public void DelChild(long id)
        {
            var entity = _childRepository.SelectById(id);
            if (entity != null) _childRepository.LogicalDelete(entity);
        }

        // ─────────────────────────────────────────────
        // 一括登録・編集
        // ─────────────────────────────────────────────

        /// <summary>一括編集用 ViewModel を取得する（親エンティティ + 子一覧）</summary>
        public DatabaseSampleBulkEditViewModel? GetBulkEditModel(long id)
        {
            var parent = _sampleRepository.SelectById(id);
            if (parent == null || parent.DelFlag) return null;

            var mapper = new MapperConfiguration(
                cfg => cfg.CreateMap<SampleEntity, DatabaseSampleBulkEditViewModel>(),
                NullLoggerFactory.Instance).CreateMapper();

            var model = mapper.Map<DatabaseSampleBulkEditViewModel>(parent);

            model.Children = _childRepository.GetChildrenByParentId(id)
                .Select(c => new DatabaseSampleBulkChildViewModel
                {
                    Id         = c.Id,
                    StringData = c.StringData,
                    IntData    = c.IntData,
                    BoolData   = c.BoolData
                }).ToList();

            return model;
        }

        /// <summary>親エンティティと子エンティティをまとめて新規登録する</summary>
        public void BulkInsert(DatabaseSampleBulkEditViewModel model, string? userName)
        {
            var parentMapper = new MapperConfiguration(
                cfg => cfg.CreateMap<DatabaseSampleBulkEditViewModel, SampleEntity>()
                          .ForMember(d => d.ApplicationUserId, o => o.MapFrom(_ => userName)),
                NullLoggerFactory.Instance).CreateMapper();

            var parent = parentMapper.Map<SampleEntity>(model);
            _sampleRepository.Insert(parent);

            // ポイント: 親の INSERT 後に parent.Id が確定するので SumpleEntityID を設定する
            foreach (var child in model.Children)
            {
                var childEntity = new SampleEntityChild
                {
                    SumpleEntityID = parent.Id,
                    StringData     = child.StringData,
                    IntData        = child.IntData,
                    BoolData       = child.BoolData
                };
                _childRepository.Insert(childEntity);
            }
        }

        /// <summary>
        /// 親エンティティと子エンティティをまとめて更新する。
        /// フォームに残っている子は UPDATE または INSERT、フォームから消えた子は論理削除する。
        /// </summary>
        public void BulkUpdate(DatabaseSampleBulkEditViewModel model)
        {
            var parent = _sampleRepository.SelectById(model.Id!)!;

            var parentMapper = new MapperConfiguration(
                cfg => cfg.CreateMap<DatabaseSampleBulkEditViewModel, SampleEntity>(),
                NullLoggerFactory.Instance).CreateMapper();

            parent = parentMapper.Map(model, parent);
            _sampleRepository.Update(parent);

            var existingChildren = _childRepository.GetChildrenByParentId(model.Id!.Value);
            var submittedIds = model.Children
                .Where(c => c.Id != null)
                .Select(c => c.Id!.Value)
                .ToHashSet();

            // フォームから消えた既存子を論理削除
            foreach (var existing in existingChildren.Where(e => !submittedIds.Contains(e.Id)))
                _childRepository.LogicalDelete(existing);

            foreach (var child in model.Children)
            {
                if (child.Id != null)
                {
                    // Id あり → 既存レコードを更新
                    var entity = _childRepository.SelectById(child.Id.Value)!;
                    entity.StringData = child.StringData;
                    entity.IntData    = child.IntData;
                    entity.BoolData   = child.BoolData;
                    _childRepository.Update(entity);
                }
                else
                {
                    // Id なし → 新規行として INSERT
                    var entity = new SampleEntityChild
                    {
                        SumpleEntityID = model.Id!.Value,
                        StringData     = child.StringData,
                        IntData        = child.IntData,
                        BoolData       = child.BoolData
                    };
                    _childRepository.Insert(entity);
                }
            }
        }

        // ─────────────────────────────────────────────
        // インポート
        // ─────────────────────────────────────────────

        public void InsertFile(ref DatabaseSampleImportViewModel model)
        {
            if (model.ImportDataFile != null)
                model.ImportErrList = ImportXlsxSample(model.ImportDataFile.OpenReadStream());
        }

        // ポイント: ClosedXML を使用した Excel ファイルのインポート処理
        private List<string> ImportXlsxSample(Stream fileStream)
        {
            var returnlist = new List<string>();

            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheets.First();
                var lastRow   = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                if (lastRow < 2) return returnlist;

                var entityList = new List<SampleEntity>();

                for (int row = 2; row <= lastRow; row++)
                {
                    int col        = 3;
                    var tempEntity = new SampleEntity();
                    tempEntity.StringData = worksheet.Cell(row, col).GetValue<string>()?.Trim() ?? "";
                    col++;
                    if (!worksheet.Cell(row, col).IsEmpty())
                        int.TryParse(worksheet.Cell(row, col).GetValue<string>(), out int intData);
                    col++;
                    if (!worksheet.Cell(row, col).IsEmpty())
                        bool.TryParse(worksheet.Cell(row, col).GetValue<string>(), out bool boolData);
                    col++;
                    Enum.TryParse(worksheet.Cell(row, col).GetValue<string>(), out SampleEnum sampleEnum);
                    tempEntity.EnumData = sampleEnum;
                    col++;
                    Enum.TryParse(worksheet.Cell(row, col).GetValue<string>(), out SampleEnum2 sampleEnum2);
                    tempEntity.EnumData2 = sampleEnum2;
                    tempEntity.SetForCreate();
                    entityList.Add(tempEntity);
                }

                if (returnlist.Count == 0)
                    _sampleRepository.BatchInsert(entityList);
            }

            return returnlist;
        }

        // ─────────────────────────────────────────────
        // エクスポート（Excel / PDF）
        // ─────────────────────────────────────────────

        // ポイント: ClosedXML を使用した Excel エクスポート
        public MemoryStream ExportFile(DatabaseSampleViewModel model, IWebHostEnvironment env)
        {
            var rowData      = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var templatePath = Path.Combine(env.WebRootPath, "Templates", "Excel", "SampleEntityTemplate.xlsx");
            var memorystream = new MemoryStream();

            XLWorkbook workbook;
            if (File.Exists(templatePath))
                workbook = new XLWorkbook(templatePath);
            else
                workbook = new XLWorkbook();

            using (workbook)
            {
                var ws       = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Sheet1");
                int intBaseRow = 2;
                foreach (var row in rowData)
                {
                    int intCol = 1;
                    ws.Cell(intBaseRow, intCol++).Value = row.Id;
                    ws.Cell(intBaseRow, intCol++).Value = row.ApplicationUserId;
                    ws.Cell(intBaseRow, intCol++).Value = row.StringData;
                    ws.Cell(intBaseRow, intCol++).Value = row.IntData;
                    ws.Cell(intBaseRow, intCol++).Value = row.BoolData;
                    ws.Cell(intBaseRow, intCol++).Value = row.EnumData.ToString();
                    ws.Cell(intBaseRow, intCol++).Value = row.EnumData2.ToString();
                    intBaseRow++;
                }
                workbook.SaveAs(memorystream);
                memorystream.Position = 0;
            }

            return memorystream;
        }

        // ポイント: QuestPDF を使用したPDF一覧出力
        public MemoryStream ExportPdf(DatabaseSampleViewModel model)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var rowData     = _sampleRepository.GetBaseQuery(_sampleRepository.GetCondModel(model.Cond)).ToList();
            var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic").FontSize(9));

                    page.Header()
                        .PaddingBottom(5)
                        .Text($"DBサンプル一覧　出力日時：{DateTime.Now:yyyy/MM/dd HH:mm}")
                        .FontSize(11).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                        });

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

                        foreach (var (row, index) in rowData.Select((r, i) => (r, i)))
                        {
                            var background = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                            static IContainer DataCellStyle(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                 .PaddingVertical(3).PaddingHorizontal(4);

                            IContainer CellWithBg(IContainer c) => DataCellStyle(c).Background(background);

                            table.Cell().Element(CellWithBg).Text(row.Id.ToString());
                            table.Cell().Element(CellWithBg).Text(row.StringData);
                            table.Cell().Element(CellWithBg).AlignRight().Text(row.IntData.ToString());
                            table.Cell().Element(CellWithBg).AlignCenter().Text(row.BoolData ? "あり" : "なし");
                            table.Cell().Element(CellWithBg).Text(row.EnumData.ToString());
                            table.Cell().Element(CellWithBg).Text(row.EnumData2.ToString());
                            table.Cell().Element(CellWithBg).Text(row.CreateDate.ToString("yyyy/MM/dd HH:mm"));
                        }
                    });

                    page.Footer().AlignRight().Text(text =>
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

        /// <summary>単体データをPDFとして出力する</summary>
        public MemoryStream? ExportPdfSingle(int id, IWebHostEnvironment env)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var entity = _sampleRepository.SelectById((long)id);
            if (entity == null) return null;

            var children       = _childRepository.GetChildrenByParentId((long)id);
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpeg", ".jpg" };
            var uploadFolder   = Path.Combine(env.WebRootPath, "Uploads", "Sample", entity.Id.ToString());
            var fileNames      = (entity.FileData ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontFamily("Noto Sans CJK JP", "Meiryo", "MS Gothic").FontSize(10));

                    page.Header().PaddingBottom(10).Column(col =>
                    {
                        col.Item().Text("DBサンプル 詳細").FontSize(14).Bold();
                        col.Item().Text($"出力日時：{DateTime.Now:yyyy/MM/dd HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    page.Content().Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
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

                            Row("ID",           entity.Id.ToString());
                            Row("ユーザーID",   entity.ApplicationUserId ?? "");
                            Row("文字列データ",  entity.StringData);
                            Row("数値データ",   entity.IntData.ToString());
                            Row("BoolData",     entity.BoolData ? "あり" : "なし");
                            Row("EnumData",     entity.EnumData.ToString());
                            Row("EnumData2",    entity.EnumData2.ToString());
                            Row("作成日時",     entity.CreateDate.ToString("yyyy/MM/dd HH:mm:ss"));
                            Row("更新日時",     entity.UpdateDate.ToString("yyyy/MM/dd HH:mm:ss"));

                            var nonImageFiles = fileNames
                                .Where(f => !imageExtensions.Contains(Path.GetExtension(f)))
                                .ToList();
                            if (nonImageFiles.Any())
                                Row("添付ファイル", string.Join("\n", nonImageFiles));
                        });

                        content.Item().PaddingTop(16).Column(childSection =>
                        {
                            childSection.Item().PaddingBottom(6)
                                .Text($"子エンティティ一覧（{children.Count} 件）")
                                .FontSize(11).Bold();

                            if (children.Count == 0)
                            {
                                childSection.Item()
                                    .Text("子エンティティはありません。")
                                    .FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                childSection.Item().Table(childTable =>
                                {
                                    childTable.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(50);
                                        cols.RelativeColumn(3);
                                        cols.ConstantColumn(70);
                                        cols.ConstantColumn(60);
                                        cols.ConstantColumn(120);
                                    });

                                    static IContainer HeaderCell(IContainer c) =>
                                        c.Background(Colors.Grey.Lighten2)
                                         .Border(1).BorderColor(Colors.Grey.Lighten1)
                                         .PaddingVertical(5).PaddingHorizontal(6)
                                         .DefaultTextStyle(x => x.Bold().FontSize(9));

                                    static IContainer DataCell(IContainer c) =>
                                        c.Border(1).BorderColor(Colors.Grey.Lighten2)
                                         .PaddingVertical(4).PaddingHorizontal(6)
                                         .DefaultTextStyle(x => x.FontSize(9));

                                    childTable.Cell().Element(HeaderCell).Text("子ID");
                                    childTable.Cell().Element(HeaderCell).Text("文字列データ");
                                    childTable.Cell().Element(HeaderCell).Text("数値データ");
                                    childTable.Cell().Element(HeaderCell).Text("BoolData");
                                    childTable.Cell().Element(HeaderCell).Text("作成日時");

                                    for (int i = 0; i < children.Count; i++)
                                    {
                                        var child = children[i];
                                        IContainer RowCell(IContainer c) =>
                                            i % 2 == 0
                                                ? DataCell(c)
                                                : c.Background(Colors.Grey.Lighten4)
                                                   .Border(1).BorderColor(Colors.Grey.Lighten2)
                                                   .PaddingVertical(4).PaddingHorizontal(6)
                                                   .DefaultTextStyle(x => x.FontSize(9));

                                        childTable.Cell().Element(RowCell).Text(child.Id.ToString());
                                        childTable.Cell().Element(RowCell).Text(child.StringData);
                                        childTable.Cell().Element(RowCell).Text(child.IntData.ToString());
                                        childTable.Cell().Element(RowCell).Text(child.BoolData ? "あり" : "なし");
                                        childTable.Cell().Element(RowCell).Text(child.CreateDate.ToString("yyyy/MM/dd HH:mm"));
                                    }
                                });
                            }
                        });

                        // ポイント: 画像ファイルのみ PDF 内に直接埋め込む
                        foreach (var fileName in fileNames.Where(f => imageExtensions.Contains(Path.GetExtension(f))))
                        {
                            var filePath = Path.Combine(uploadFolder, fileName);
                            if (!File.Exists(filePath)) continue;

                            var imageBytes = File.ReadAllBytes(filePath);

                            content.Item().PaddingTop(12).Column(imgCol =>
                            {
                                imgCol.Item().Text(fileName).FontSize(9).FontColor(Colors.Grey.Medium);
                                imgCol.Item().MaxHeight(300).Image(imageBytes).FitArea();
                            });
                        }
                    });
                });
            }).GeneratePdf(memoryStream);

            memoryStream.Position = 0;
            return memoryStream;
        }

        // ポイント: AutoMapper の前付け名・後付け名マッピングのサンプル
        public DatabaseMapperUsageSampleViewModel GetMapperUsage()
        {
            var model = new DatabaseMapperUsageSampleViewModel();
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.RecognizeDestinationPrefixes("MapperUsage");
                cfg.RecognizeDestinationPostfixes("WithMapper");
                cfg.CreateMap<SampleEntity, DatabaseMapperUsageSampleDataModel>()
                   .ForMember(dest => dest.LastUpdatedBy, opt => opt.MapFrom(src => src.UpdateApplicationUserId))
                   .ForMember(dest => dest.LastUpdated,   opt => opt.MapFrom(src => src.UpdateDate));
            }, NullLoggerFactory.Instance);

            model.Data = _sampleRepository
                .GetQueryAs<DatabaseMapperUsageSampleDataModel>(config: mapperConfig)
                .ToList();
            return model;
        }
    }
}
