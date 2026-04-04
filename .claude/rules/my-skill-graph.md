# My-Skill-Graph 連携ルール

## ボルトパス

`C:/Users/harne/iCloudDrive/My-Skill-Graph`

---

## Orient（セッション開始）

SessionStart フックが自動注入する：
- `self/identity.md` — このエージェントが何者か
- `self/goals.md` — 現在進行中のスレッドと次のアクション

追加コンテキストが必要な場合はファイルを直接読むこと：
- `self/methodology.md` — 作業方法・原則
- `decisions/DevNext-index.md` — DevNext 設計判断のハブマップ

---

## Work（設計判断の記録タイミング）

以下のいずれかが発生したとき `decisions/` に命題文ノートを作成すること：
- 技術選択・アーキテクチャ判断をしたとき
- リファクタリング方針を決めたとき
- 代替案を検討して棄却したとき
- 設計上のトレードオフを意識したとき

### ノート命名規則

タイトルは「このノートが主張するのは [タイトル]」が成立する命題文にする。
（例: `SiteEntityBaseを全エンティティに継承させたのは監査カラムを統一するため.md`）

### ノートフォーマット

```markdown
---
project: DevNext
date: YYYY-MM-DD
status: active
---

# [命題文タイトル]

[判断の背景・文脈を2-3文で]

## 選択した理由

## 検討した代替案（なければ省略）

## 関連判断

- [[関連ノートタイトル]] — 繋がりの説明

## 案件

- [[decisions/DevNext-index]]
```

新しい判断ノートを作成したら `decisions/DevNext-index.md` の「コアアイデア」セクションにリンクを追加すること。

### 戦略メモ（ポートフォリオ・就活観点）

DevNext 開発から就活・ポートフォリオに繋がる気づきは `strategies/` に記録する。

---

## Persist（セッション終了前に必ず実行）

1. **`self/goals.md` を更新する** — 完了したこと・次のアクション候補
2. **設計判断を記録する** — `decisions/` に命題文で
3. **戦略メモを記録する** — ポートフォリオ観点の気づきがあれば `strategies/` に
