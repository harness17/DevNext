---
name: export-docs
description: docs/ フォルダの Markdown ドキュメントを Word / Excel ファイルに出力する。ドキュメント更新後に実行して .docx / .xlsx を最新に保つ。
---

`docs/` フォルダの Markdown ソースをもとに、対応する Word / Excel ファイルを生成してください。

## Pandoc パス

```bash
PANDOC="/c/Users/harne/AppData/Local/Microsoft/WinGet/Packages/JohnMacFarlane.Pandoc_Microsoft.Winget.Source_8wekyb3d8bbwe/pandoc-3.9.0.2/pandoc.exe"
```

## ドキュメント形式の対応表

| Markdown ソース | 出力形式 | 出力ファイル名 | ツール |
|----------------|---------|-------------|--------|
| 基本設計書.md | Word | 基本設計書.docx | Pandoc |
| 詳細設計書.md | Word | 詳細設計書.docx | Pandoc |
| テスト設計書.md | Word | テスト設計書.docx | Pandoc |
| API仕様書.md | Word | API仕様書.docx | Pandoc |
| 画面設計書.md | Word | 画面設計書.docx | Pandoc |
| デプロイ手順書.md | Word | デプロイ手順書.docx | Pandoc |
| CommonLibrary利用ガイド.md | Word | CommonLibrary利用ガイド.docx | Pandoc |
| DB設計書.md | Excel | DB設計書.xlsx | openpyxl |
| テスト設計書.md（テストケース部分） | Excel | テストケース一覧.xlsx | openpyxl |

## Word ファイルの生成（Pandoc）

### 1ファイル変換

```bash
PANDOC="/c/Users/harne/AppData/Local/Microsoft/WinGet/Packages/JohnMacFarlane.Pandoc_Microsoft.Winget.Source_8wekyb3d8bbwe/pandoc-3.9.0.2/pandoc.exe"
"$PANDOC" docs/基本設計書.md -o docs/基本設計書.docx
```

### 全 Word ドキュメント一括変換

```bash
PANDOC="/c/Users/harne/AppData/Local/Microsoft/WinGet/Packages/JohnMacFarlane.Pandoc_Microsoft.Winget.Source_8wekyb3d8bbwe/pandoc-3.9.0.2/pandoc.exe"
for f in docs/基本設計書.md docs/詳細設計書.md docs/テスト設計書.md docs/API仕様書.md docs/画面設計書.md docs/デプロイ手順書.md docs/CommonLibrary利用ガイド.md; do
  "$PANDOC" "$f" -o "${f%.md}.docx" && echo "OK: $f"
done
```

### スタイルテンプレートを適用する場合

```bash
"$PANDOC" docs/基本設計書.md --reference-doc=docs/template.docx -o docs/基本設計書.docx
```

`template.docx` は Word で開いてスタイルを編集したものを `docs/` に置く。

## Excel ファイルの生成（md_to_xlsx.py）

`scripts/md_to_xlsx.py` を使う。Markdown をそのまま xlsx に変換する汎用スクリプト。

```bash
# DB設計書
python scripts/md_to_xlsx.py docs/DB設計書.md docs/DB設計書.xlsx

# テストケース一覧
python scripts/md_to_xlsx.py docs/テスト設計書.md docs/テストケース一覧.xlsx
```

**変換ルール（自動適用）:**

| Markdown 要素 | xlsx 出力 |
|--------------|----------|
| `# H1` | 濃紺タイトル行（シート先頭） |
| `## H2` | 新しいシートを作成 |
| `### H3` | 薄青のセクションタイトル行 |
| テーブルヘッダー行 | 青背景・白文字 |
| テーブルデータ行（偶数） | 薄青交互 |
| 通常テキスト | 薄グレー行 |

## バリデーション

```bash
python -c "
import zipfile, os, glob
for f in glob.glob('docs/*.docx') + glob.glob('docs/*.xlsx'):
    with zipfile.ZipFile(f) as z:
        print(f'OK {os.path.basename(f)}: {len(z.namelist())} entries, {os.path.getsize(f):,} bytes')
"
```

## 部分更新の場合

特定ファイルのみ更新する場合は対象の変換コマンドだけ実行する。
例: `API仕様書.md` を更新した場合は `API仕様書.docx` のみ再生成する。

```bash
"$PANDOC" docs/API仕様書.md -o docs/API仕様書.docx
```

## 注意事項

- `docs/*.md` が正。`.docx` / `.xlsx` は派生物。
- Markdown を先に更新 → Pandoc/openpyxl で再生成の順で行う。
- Pandoc がパスに通っていない場合は上記のフルパスを使う。
