# IU 4バー上昇戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

IU 4バー上昇戦略は、価格がSuperTrendインジケーターの上方にあるときに4本連続の強気ローソク足の後に買うロングのみのアプローチです。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 4本連続の強気ローソク足とSuperTrend上方での終値。
- **エグジット条件**: SuperTrend下方での終値。
- **ストップ**: なし。
- **デフォルト値**:
  - `SupertrendLength` = 14
  - `SupertrendMultiplier` = 1
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: SuperTrend
  - 複雑さ: 低
  - リスクレベル: 中
