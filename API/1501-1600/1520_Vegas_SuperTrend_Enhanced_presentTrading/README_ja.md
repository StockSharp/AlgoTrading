# Vegas SuperTrend Enhanced presentTrading戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Vegasチャネルと調整されたSuperTrendを組み合わせます。
ボラティリティベースの乗数を用いてSuperTrendが方向を転換したときにエントリーします。

## 詳細

- **エントリー条件**: 調整されたSuperTrendによるトレンド転換の検出
- **ロング/ショート**: 両方（設定可能）
- **エグジット条件**: 反対方向のトレンド転換
- **ストップ**: なし
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `VegasWindow` = 100
  - `SuperTrendMultiplier` = 5
  - `VolatilityAdjustment` = 5
  - `TradeDirection` = "Both"
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
