using Site.Common;

namespace Site.Models
{
    // ポイント: 親モデルが子モデルを持つ階層構造
    //           Child は単体パーシャルビュー用、ChildList はリスト・ソートサンプル用
    public class PartialSampleViewModel
    {
        // 単体パーシャルビューに渡す子モデル
        public PartialSampleChildModel Child { get; set; } = new PartialSampleChildModel();
        // ポイント: リスト系パーシャル・ソートサンプルで使用する子モデルのリスト
        //           MVC のモデルバインディングで ChildList[0].Text 形式のフォームデータをバインドするため
        //           プロパティ名を ChildList にする必要がある（View 側の name 属性と一致させる）
        public List<PartialSampleChildModel> ChildList { get; set; } = new List<PartialSampleChildModel>();
    }

    public class PartialSampleChildModel
    {
        public string? Text { get; set; }
        public int Number { get; set; }
        // ポイント: Order はドラッグ&ドロップ並び替え時に JavaScript 側で更新する順序値
        //           保存時はこの値をDBに記録して次回表示時に並び替えに使う
        public int Order { get; set; }
        public SampleEnum Enum { get; set; }
    }
}
