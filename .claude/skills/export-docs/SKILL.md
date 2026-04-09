---
name: export-docs
description: docs/ フォルダの Markdown ドキュメントを Word / Excel ファイルに出力する。ドキュメント更新後に実行して .docx / .xlsx を最新に保つ。
---

`docs/` フォルダの Markdown ソースをもとに、対応する Word / Excel ファイルを生成してください。

## ドキュメント形式の対応表

| Markdown ソース | 出力形式 | 出力ファイル名 | 判断理由 |
|----------------|---------|-------------|---------|
| 基本設計書.md | Word | 基本設計書.docx | 文書スタイル（説明・機能一覧・画面一覧） |
| 詳細設計書.md | Word | 詳細設計書.docx | 文書スタイル（コード例・設計説明） |
| テスト設計書.md | Word | テスト設計書.docx | 文書スタイル（方針・手順） |
| API仕様書.md | Word | API仕様書.docx | 文書スタイル（エンドポイント仕様・JSON例） |
| 画面設計書.md | Word | 画面設計書.docx | 文書スタイル（画面要素・操作フロー） |
| デプロイ手順書.md | Word | デプロイ手順書.docx | 文書スタイル（手順・コマンド） |
| CommonLibrary利用ガイド.md | Word | CommonLibrary利用ガイド.docx | 文書スタイル（API 説明・コード例） |
| DB設計書.md | Excel | DB設計書.xlsx | テーブル重視（テーブル定義・Enum定義） |
| テスト設計書.md（テストケース部分） | Excel | テストケース一覧.xlsx | テーブル重視（テストケース管理） |

## 形式選択の判断基準

- **Word** → 説明文・手順・コード例が主体。読み物として使う。
- **Excel** → テーブル定義・テストケース一覧など、フィルタ・ソートして参照するデータが主体。

## 実行手順

### 1. 対象ファイルの確認

更新が必要なファイルを特定し、対応する生成スクリプトを作成する。

### 2. Word ファイルの生成（docx-js を使用）

```bash
# Windows: グローバル npm パッケージの NODE_PATH を設定して実行
NODE_PATH="C:\Users\harne\AppData\Roaming\npm\node_modules" node 生成スクリプト.js
```

**重要: Windows では `NODE_PATH` の設定が必須。** グローバルインストールした `docx` パッケージが見つからない場合に発生するエラーへの対処。

```bash
# docx パッケージが未インストールの場合
npm install -g docx
```

**docx-js の基本パターン:**

```javascript
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
  ShadingType, VerticalAlign, PageNumber, LevelFormat, PageBreak
} = require('docx');
const fs = require('fs');

// A4 サイズ（DXA: 1440 = 1 inch）
const PAGE = { size: { width: 11906, height: 16838 }, margin: { top: 1440, right: 1134, bottom: 1440, left: 1134 } };
const CONTENT_W = 11906 - 1134 * 2; // 9638 DXA

// 箇条書きは LevelFormat.BULLET を使う（unicode 文字を直接書かない）
const NUMBERING = {
  config: [{
    reference: 'bullets',
    levels: [{ level: 0, format: LevelFormat.BULLET, text: '\u2022', alignment: AlignmentType.LEFT,
      style: { paragraph: { indent: { left: 720, hanging: 360 } } } }]
  }]
};

const doc = new Document({ numbering: NUMBERING, sections: [{ properties: { page: PAGE }, children: [...] }] });
Packer.toBuffer(doc).then(buf => fs.writeFileSync('output.docx', buf));
```

### 3. Excel ファイルの生成（openpyxl を使用）

```python
import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side

wb = openpyxl.Workbook()
# シートを追加・整形して保存
wb.save('output.xlsx')
```

**Excel の色分け規則:**

| 用途 | fill color |
|------|-----------|
| ヘッダー行 | `2E75B6`（青） |
| 交互行 | `DEEAF1`（薄青） |
| 主キー行 | `E2EFDA`（薄緑） |
| 外部キー行 | `FCE4D6`（薄橙） |
| セクションタイトル | `BDD7EE`（薄青） |
| タイトル行 | `1F4E79`（濃紺） |

### 4. バリデーション

生成されたファイルが有効な Office ファイルか確認する。docx/xlsx は ZIP 形式なので、解凍できれば構造的には正常。

```bash
# docx: サイズ確認 + ZIP 構造チェック
python -c "
import zipfile, sys
path = sys.argv[1]
with zipfile.ZipFile(path) as z:
    names = z.namelist()
    print(f'OK: {len(names)} entries in {path}')
    print([n for n in names if n.endswith('.xml')][:5])
" output.docx
```

ファイルサイズが 0 bytes でなく、`[Content_Types].xml` などが含まれていれば正常。

### 5. スクリプトのクリーンアップ

生成後は一時スクリプトを削除する。

```bash
rm 生成スクリプト.js
rm 生成スクリプト.py
```

## 部分更新の場合

特定のドキュメントのみ更新する場合は、対象ファイルのスクリプトのみ作成・実行する。例えば `DB設計書.md` が更新された場合は `DB設計書.xlsx` のみ再生成する。

## 注意事項

- 生成した Office ファイルは `docs/` フォルダに出力する
- Markdown ソース（`.md`）が正として Office ファイルは派生物として扱う
- Markdown と Office ファイルの内容が乖離しないよう、機能追加・変更時は両方を更新する
