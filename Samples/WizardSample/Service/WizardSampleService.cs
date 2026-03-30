using WizardSample.Common;
using WizardSample.Data;
using WizardSample.Entity;
using WizardSample.Models;
using WizardSample.Repository;

namespace WizardSample.Service
{
    /// <summary>
    /// 多段階フォームサンプルサービス
    /// ウィザード完了時にセッションモデルのデータを DB に保存する
    /// </summary>
    public class WizardSampleService
    {
        private readonly WizardEntityRepository _wizardRepository;

        public WizardSampleService(WizardSampleDbContext context)
        {
            _wizardRepository = new WizardEntityRepository(context);
        }

        /// <summary>
        /// 検索条件に基づいてウィザード登録データ一覧を取得する
        /// </summary>
        /// <param name="model">一覧 ViewModel（検索条件・ページング情報を含む）</param>
        /// <returns>データ取得後の ViewModel</returns>
        public WizardSampleListViewModel GetWizardEntityList(WizardSampleListViewModel model)
        {
            // ViewModel の検索条件をリポジトリ用 CondModel に変換する
            model.Cond.Pager.page         = model.Page;
            model.Cond.Pager.sort         = model.Sort ?? "";
            model.Cond.Pager.sortdir      = model.SortDir ?? "";
            model.Cond.Pager.recoedNumber = model.RecordNum;

            var cond = _wizardRepository.GetCondModel(model.Cond);
            model.RowData = _wizardRepository.GetWizardEntityList(cond);
            return model;
        }

        /// <summary>
        /// ウィザードセッションモデルをエンティティに変換して保存する
        /// </summary>
        /// <param name="session">全ステップのデータ</param>
        public void SaveWizardData(WizardSessionModel session)
        {
            var entity = new WizardEntity
            {
                Name        = session.Name,
                Email       = session.Email,
                Phone       = session.Phone,
                Subject     = session.Subject,
                Content     = session.Content,
                Category    = session.Category,
                DesiredDate = session.DesiredDate
            };
            _wizardRepository.Insert(entity);
        }
    }
}
