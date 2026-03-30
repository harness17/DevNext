using Dev.CommonLibrary.Common;
using FileSample.Common;
using FileSample.Entity;
using FileSample.Models;
using FileSample.Repository;

namespace FileSample.Service
{
    /// <summary>
    /// ファイル管理サービス
    /// ファイルのアップロード・ダウンロード・削除とDBメタ情報管理を担う
    /// </summary>
    public class FileManagementService
    {
        private readonly FileEntityRepository _fileRepository;

        // ファイル保存先（wwwroot 相対パス）
        private const string UploadFolder = "Uploads/Files";

        // 許可する拡張子
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".ppt", ".pptx",
            ".png", ".jpeg", ".jpg", ".gif", ".txt", ".csv", ".zip"
        };

        // 最大ファイルサイズ: 10 MB
        private const long MaxFileSize = 1024L * 1024 * 10;

        public FileManagementService(FileEntityRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        /// <summary>
        /// ファイル一覧を検索条件付きで取得する
        /// </summary>
        public FileManagementViewModel GetFileList(FileManagementViewModel model)
        {
            if (model.Cond == null) model.Cond = new FileManagementCondViewModel();
            LocalUtil.SetPager(model.Cond, model);
            model.RowData = _fileRepository.GetFileEntityList(_fileRepository.GetCondModel(model.Cond));
            return model;
        }

        /// <summary>
        /// ファイルをアップロードし、メタ情報を DB に保存する
        /// </summary>
        /// <param name="file">アップロードするファイル</param>
        /// <param name="description">説明（任意）</param>
        /// <param name="env">物理パス解決用ホスト環境</param>
        /// <returns>エラーメッセージ文字列。成功時は null</returns>
        public string? UploadFile(IFormFile file, string? description, IWebHostEnvironment env)
        {
            // ファイルバリデーション
            if (file.Length == 0)
                return "ファイルが空です。";
            if (file.Length > MaxFileSize)
                return $"ファイルサイズが上限（{MaxFileSize / 1024 / 1024}MB）を超えています。";

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return $"許可されていない拡張子です。（許可: {string.Join(", ", AllowedExtensions)}）";

            if (!Util.IsSafePath(file.FileName, true))
                return "ファイル名に不正な文字が含まれています。";

            // 保存先フォルダを作成
            var saveFolderPath = Path.Combine(env.WebRootPath, UploadFolder);
            Directory.CreateDirectory(saveFolderPath);

            // GUID をファイル名に付加して一意性・安全性を確保
            var savedFileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(saveFolderPath, savedFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                file.CopyTo(stream);

            // DB にメタ情報を登録
            var entity = new FileEntity
            {
                OriginalFileName = Util.SetFileName(file.FileName),
                SavedFileName    = savedFileName,
                FileSize         = file.Length,
                ContentType      = file.ContentType,
                Description      = description
            };
            _fileRepository.Insert(entity);

            return null;
        }

        /// <summary>
        /// ダウンロード用にエンティティとファイルパスを取得する
        /// </summary>
        /// <returns>(エンティティ, 物理ファイルパス) のタプル。存在しない場合は null</returns>
        public (FileEntity entity, string filePath)? GetForDownload(long id, IWebHostEnvironment env)
        {
            var entity = _fileRepository.SelectById(id);
            if (entity == null || entity.DelFlag) return null;

            var filePath = Path.Combine(env.WebRootPath, UploadFolder, entity.SavedFileName);
            if (!File.Exists(filePath)) return null;

            return (entity, filePath);
        }

        /// <summary>
        /// ファイルを論理削除し、物理ファイルも削除する
        /// </summary>
        public bool DeleteFile(long id, IWebHostEnvironment env)
        {
            var entity = _fileRepository.SelectById(id);
            if (entity == null || entity.DelFlag) return false;

            // 物理ファイルを先に削除する
            var filePath = Path.Combine(env.WebRootPath, UploadFolder, entity.SavedFileName);
            if (File.Exists(filePath)) File.Delete(filePath);

            // DB を論理削除
            _fileRepository.LogicalDelete(entity);
            return true;
        }

        /// <summary>
        /// 削除確認用にエンティティを取得する
        /// </summary>
        public FileEntity? GetById(long id)
        {
            var entity = _fileRepository.SelectById(id);
            return (entity?.DelFlag == false) ? entity : null;
        }
    }
}
