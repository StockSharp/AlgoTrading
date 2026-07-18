# 流動性ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ピボット高値と安値によって定義された最近の価格レンジからのブレイクアウトを取引します。価格が前のレンジの極値を超えてクローズしたときにポジションをオープンします。オプションのストップロスはSuperTrendラインまたは固定パーセンテージを使用できます。

## 詳細

- **エントリー条件**:
  - `終値 > 前レンジ高値` → ロング
  - `終値 < 前レンジ安値` → ショート
- **ロング/ショート**: 設定可能（ロング、ショート、両方）。
- **エグジット条件**: 反対方向のブレイクアウトまたはストップロス。
- **ストップ**: SuperTrendまたは固定パーセンテージ。
- **デフォルト値**:
  - `PivotLength` = 12
  - `StopLoss` = SuperTrend
  - `FixedPercentage` = 0.1
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Highest, Lowest, SuperTrend
  - ストップ: オプション
  - 複雑さ: 低
  - 時間軸: 1h
