# ロジスティック関数付きMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ロジスティック関数付きMA戦略は、速いMAと遅いMAを使ってエントリーし、パーセントベースまたはロジスティック確率ベースのエグジットをサポートする移動平均戦略です。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 終値 > 速いMA かつ 速いMA > 遅いMA。
  - **ショート**: 終値 < 速いMA かつ 速いMA < 遅いMA。
- **エグジット条件**: パーセント目標またはロジスティック確率閾値。
- **ストップ**: パーセントベースまたはロジスティック確率ベースのエグジット。
- **デフォルト値**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングとショート
  - インジケーター: MA
  - 複雑さ: 低
  - リスクレベル: 中
