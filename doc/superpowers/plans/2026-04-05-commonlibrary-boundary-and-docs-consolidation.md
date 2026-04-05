# CommonLibrary 責務境界定義 & ドキュメントフォルダ統合 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CommonLibrary の責務境界をルール化し、`docs/` を `doc/` に統合し、`util.cs` を責務ごとに分割する。

**Architecture:** ドキュメントフォルダ統合はファイル移動 + 参照更新。ルール追加は `.claude/rules/` への新規ファイル作成 + `CLAUDE.md` への参照追加。`util.cs` 分割は既存クラスを個別ファイルに切り出す純粋なリファクタリング。

**Tech Stack:** Bash（ファイル移動）、C#（util.cs 分割）

---

## ファイルマップ

| 操作 | パス |
|------|------|
| 作成 | `.claude/rules/commonlibrary-boundary.md` |
| 修正 | `CLAUDE.md`（rules 参照追加、docs/ → doc/ 修正） |
| 作成 | `CommonLibrary/Common/EnumUtility.cs` |
| 作成 | `CommonLibrary/Common/SelectListUtility.cs` |
| 作成 | `CommonLibrary/Common/CookieUtility.cs` |
| 作成 | `CommonLibrary/Common/Util.cs` |
| 削除 | `CommonLibrary/Common/util.cs` |
| 移動 | `docs/**/*` → `doc/**/*`（サブフォルダ構造を維持） |

---

## Task 1: docs/ → doc/ へのファイル移動

**Files:**
- 移動: `docs/superpowers/` → `doc/superpowers/`
- 移動: `docs/recipes/` → `doc/recipes/`
- 移動: `docs/setup.md` → `doc/setup.md`
- 移動: `docs/customization.md` → `doc/customization.md`
- 削除: `docs/` フォルダ

- [ ] **Step 1: ディレクトリを作成してファイルを移動する**

```bash
cd H:/ClaudeCode/DevNext

# superpowers/specs
mkdir -p doc/superpowers/specs
cp docs/superpowers/specs/*.md doc/superpowers/specs/

# superpowers/plans（本計画ファイルは既に doc/ にあるため対象外）
cp docs/superpowers/plans/*.md doc/superpowers/plans/

# recipes
mkdir -p doc/recipes
cp docs/recipes/*.md doc/recipes/

# ルートドキュメント
cp docs/setup.md doc/setup.md
cp docs/customization.md doc/customization.md
```

- [ ] **Step 2: コピーを確認する**

```bash
ls doc/superpowers/specs/
ls doc/superpowers/plans/
ls doc/recipes/
ls doc/
```

期待値: specs/plans/recipes すべてのファイルが doc/ 配下に存在すること

- [ ] **Step 3: docs/ を削除する**

```bash
rm -rf H:/ClaudeCode/DevNext/docs
```

- [ ] **Step 4: git でファイル移動をステージする**

```bash
cd H:/ClaudeCode/DevNext
git add doc/superpowers/ doc/recipes/ doc/setup.md doc/customization.md
git rm -r docs/
```

- [ ] **Step 5: コミット**

```bash
git commit -m "refactor: docs/ を doc/ に統合"
```

---

## Task 2: CLAUDE.md の参照を更新する

**Files:**
- 修正: `CLAUDE.md`（`docs/` → `doc/` に2箇所修正）

- [ ] **Step 1: CLAUDE.md の docs/ 参照を修正する**

`CLAUDE.md` の以下の2箇所を修正する：

変更前:
```
docs/             ← 設計書・実装計画
```
変更後:
```
doc/              ← 設計書・実装計画
```

変更前:
```
- パターンの参照は `docs/recipes/` のドキュメントで行う
```
変更後:
```
- パターンの参照は `doc/recipes/` のドキュメントで行う
```

- [ ] **Step 2: コミット**

```bash
git add CLAUDE.md
git commit -m "docs: CLAUDE.md の docs/ 参照を doc/ に修正"
```

---

## Task 3: CommonLibrary 境界ルールを追加する

**Files:**
- 作成: `.claude/rules/commonlibrary-boundary.md`
- 修正: `CLAUDE.md`（`@rules/commonlibrary-boundary.md` を追加）

- [ ] **Step 1: `.claude/rules/commonlibrary-boundary.md` を作成する**

```markdown
# CommonLibrary 責務境界ルール

## 原則

> **「このライブラリを別プロジェクトに持っていっても使える」ものだけを置く。**

## ホワイトリスト（入れていいカテゴリ）

| カテゴリ | 具体例 |
|---------|-------|
| Entity 基底 | `EntityBase`, `SiteEntityBase`, `IEntity` |
| Identity エンティティ | `ApplicationUser`, `ApplicationRole`, `UserPreviousPassword` |
| Repository 基底 | `RepositoryBase`, `IRepository` |
| ページング | `CommonListPagerModel`, `CommonListSummaryModel` |
| ロギング | `Logger`, `LogModel`, `ILogModel` |
| 属性 | `AccessLogAttribute`, `SubValueAttribute`, `FileAttribute` |
| 拡張メソッド | `StringExtensions`, `EnumExtensions` など |
| バッチ基底 | `IBatch`, `BatchService` |

## ブラックリスト（入れてはいけないもの）

| NG の例 | 理由 |
|--------|------|
| アプリ固有の定数・Enum | プロジェクト依存 |
| Sample 固有のヘルパー | Sample は CommonLibrary を使う側 |
| 特定機能の業務ロジック | 呼び出し側プロジェクトに書く |

## 既存クラスへの追記ルール

> **既存クラスへの追記が許されるのは、そのクラスの責務名で説明できるときだけ。**
> 説明できなければ新クラスを作る。

## グレーゾーンの判断ゲート（3問チェック）

ホワイトリストに当てはまらないものを追加するとき：

```
① 他プロジェクトに持っていっても使えるか？
     NO → CommonLibrary には入れない。呼び出し側に書く。

② 単一の責務に収まるか？
     NO → CommonLibrary には入れない。責務を分解してから再検討。

③ 独立したクラスとして成立するか？
     NO → 既存クラスの責務名で説明できるか確認。できなければ新クラスを作る。
     YES → 新クラスとして追加する。
```
```

- [ ] **Step 2: CLAUDE.md に参照を追加する**

`CLAUDE.md` のルール参照セクションに追加する：

変更前:
```
@rules/coding-policy.md
```
変更後:
```
@rules/coding-policy.md
@rules/commonlibrary-boundary.md
```

- [ ] **Step 3: コミット**

```bash
git add CLAUDE.md .claude/rules/commonlibrary-boundary.md
git commit -m "docs: CommonLibrary 責務境界ルールを追加"
```

---

## Task 4: util.cs を責務ごとに分割する

**Files:**
- 作成: `CommonLibrary/Common/EnumUtility.cs`
- 作成: `CommonLibrary/Common/SelectListUtility.cs`
- 作成: `CommonLibrary/Common/CookieUtility.cs`
- 作成: `CommonLibrary/Common/Util.cs`
- 削除: `CommonLibrary/Common/util.cs`

- [ ] **Step 1: EnumUtility.cs を作成する**

`CommonLibrary/Common/EnumUtility.cs` を作成：

```csharp
using Dev.CommonLibrary.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// Enum関連Utility
    /// </summary>
    public static class EnumUtility
    {
        public static string GetEnumDisplay<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return string.Empty;
            var attrs = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attrs == null || attrs.Length == 0) return value;
            var attr = attrs[0];
            if (attr.ResourceType != null) return attr.GetName() ?? value;
            return attr.Name ?? value;
        }

        public static string GetEnumSubValue<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return string.Empty;
            var attrs = fieldInfo.GetCustomAttributes(typeof(SubValueAttribute), false) as SubValueAttribute[];
            if (attrs == null || attrs.Length == 0) return string.Empty;
            return attrs[0].SubValue;
        }

        public static int GetEnumDisplayOrder<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return 0;
            var fieldInfo = type.GetField(name);
            if (fieldInfo == null) return 0;
            var attrs = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attrs == null || attrs.Length == 0) return 0;
            return attrs[0].Order;
        }

        public static string GetEnumDescription<T>(string value) where T : struct
        {
            Type type = typeof(T);
            var name = GetEnumName(type, value);
            if (name == null) return string.Empty;
            var field = type.GetField(name);
            if (field == null) return string.Empty;
            var customAttribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return customAttribute.Length > 0 ? ((DescriptionAttribute)customAttribute[0]).Description : name;
        }

        private static string? GetEnumName(Type type, string value)
        {
            return Enum.GetNames(type)
                .Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
        }

        public static string GetDescription(Type T, string name)
        {
            var attributes = (DescriptionAttribute[])T.GetField(name)!
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            var description = attributes.Select(n => n.Description).FirstOrDefault();
            return string.IsNullOrEmpty(description) ? name : description!;
        }
    }
}
```

- [ ] **Step 2: SelectListUtility.cs を作成する**

`CommonLibrary/Common/SelectListUtility.cs` を作成：

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// SelectList関連Utility
    /// </summary>
    public static class SelectListUtility
    {
        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = area,
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItem<T>(List<T> obj) where T : struct
        {
            var list = new List<SelectListItem>();
            var targetAreas = obj.Select(s => s.ToString()).ToList();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                if (targetAreas.Contains(area))
                {
                    list.Add(new SelectListItem { Value = area, Text = EnumUtility.GetEnumDisplay<T>(area) });
                }
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemToSubValue<T>() where T : struct
        {
            var list = new List<SelectListItem>();
            foreach (var area in Enum.GetNames(typeof(T)))
            {
                list.Add(new SelectListItem
                {
                    Value = EnumUtility.GetEnumSubValue<T>(area),
                    Text = EnumUtility.GetEnumDisplay<T>(area)
                });
            }
            return list;
        }

        public static IEnumerable<SelectListItem> GetEnumSelectListItemOrder<T>() where T : struct
        {
            var sortlist = Enum.GetNames(typeof(T))
                .Select(area => new { Name = area, Text = EnumUtility.GetEnumDisplay<T>(area), Order = EnumUtility.GetEnumDisplayOrder<T>(area) })
                .OrderBy(x => x.Order)
                .ToList();

            return sortlist.Select(area => new SelectListItem { Value = area.Name, Text = area.Text }).ToList();
        }

        public static IEnumerable<SelectListItem> GetNumberSelectList(int startNumber, int maxNumber, int step = 1, string format = "")
        {
            var list = new List<SelectListItem>();
            for (int i = startNumber; i <= maxNumber; i += step)
            {
                list.Add(new SelectListItem { Value = i.ToString(format), Text = i.ToString(format) });
            }
            return list;
        }
    }
}
```

- [ ] **Step 3: CookieUtility.cs を作成する**

`CommonLibrary/Common/CookieUtility.cs` を作成：

```csharp
using Microsoft.AspNetCore.Http;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// Cookie操作Utility (ASP.NET Core版)
    /// </summary>
    public static class CookieUtility
    {
        public static string? GetCookieValueByKey(IRequestCookieCollection cookies, string key)
        {
            cookies.TryGetValue(key, out var value);
            return value;
        }

        public static void SetCookie(IResponseCookies cookies, string key, string value)
        {
            cookies.Append(key, value, new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.Now.AddMonths(1)
            });
        }

        public static void DeleteCookie(IRequestCookieCollection requestCookies, IResponseCookies responseCookies, string key)
        {
            if (!requestCookies.ContainsKey(key)) return;
            responseCookies.Append(key, "", new CookieOptions
            {
                Path = "/",
                Expires = DateTimeOffset.Now.AddMonths(-1)
            });
        }
    }
}
```

- [ ] **Step 4: Util.cs を作成する**

`CommonLibrary/Common/Util.cs` を作成：

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Dev.CommonLibrary.Common
{
    /// <summary>
    /// 共通関数クラス
    /// </summary>
    public static class Util
    {
        public static string calcMd5(string srcStr)
        {
            byte[] srcBytes = Encoding.UTF8.GetBytes(srcStr);
            byte[] destBytes = MD5.HashData(srcBytes);
            var sb = new StringBuilder();
            foreach (byte b in destBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static string SetFileName(string strFileName)
        {
            string targettext = "\\";
            if (strFileName.Contains(targettext))
            {
                int lastindex = strFileName.LastIndexOf(targettext);
                strFileName = strFileName.Substring(lastindex + targettext.Length);
            }
            return strFileName;
        }

        public static bool IsSafePath(string path, bool isFileName)
        {
            if (string.IsNullOrEmpty(path)) return false;
            char[] invalidChars = isFileName
                ? Path.GetInvalidFileNameChars()
                : Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0) return false;
            if (Regex.IsMatch(path, ConstRegExpr.InValidFileName, RegexOptions.IgnoreCase)) return false;
            return true;
        }

        public static CommonListSummaryModel CreateSummary(CommonListPagerModel pager, int totalRecords, string listSummaryFormat)
        {
            int pageIndex = pager.page - 1;
            int firstRecord = (pageIndex * pager.recoedNumber) + 1;
            int endRecord = firstRecord - 1 + pager.recoedNumber;
            if (firstRecord <= totalRecords && totalRecords <= endRecord) endRecord = totalRecords;
            string summary = string.Format(listSummaryFormat, totalRecords, firstRecord, endRecord);
            return new CommonListSummaryModel(pager.page, totalRecords, firstRecord, endRecord, summary);
        }
    }
}
```

- [ ] **Step 5: 元の util.cs を削除する**

```bash
rm H:/ClaudeCode/DevNext/CommonLibrary/Common/util.cs
```

- [ ] **Step 6: ビルドを確認する**

```bash
cd H:/ClaudeCode/DevNext && dotnet build DevNext.sln
```

期待値: `Build succeeded` （警告・エラーなし）

- [ ] **Step 7: コミット**

```bash
git add CommonLibrary/Common/EnumUtility.cs
git add CommonLibrary/Common/SelectListUtility.cs
git add CommonLibrary/Common/CookieUtility.cs
git add CommonLibrary/Common/Util.cs
git rm CommonLibrary/Common/util.cs
git commit -m "refactor: util.cs を責務ごとに分割"
```

---

## Task 5: spec ファイルを doc/ に移動して完了

今回のブレインストーミングで生成した spec は `docs/superpowers/specs/` に保存されているため、Task 1 で移動済みになる。  
ただし、この計画ファイル自体は最初から `doc/superpowers/plans/` に保存済みのため対応不要。
