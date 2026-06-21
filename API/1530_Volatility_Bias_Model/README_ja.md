# ボラティリティ・バイアス・モデル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ウィンドウ内の上昇終値と下落終値をカウントし、ボラティリティが十分な場合に支配的なバイアスの方向に取引します。ATRターゲットを使用し、最大バー数に達した後にポジションを終了します。

## 詳細
- **エントリー条件**: ロングは `BiasThreshold` を超えるバイアス比率、ショートは `1 - BiasThreshold` を下回るバイアス比率、かつレンジが `RangeMin` を超えること。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップ、利益確定、または `MaxBars` 到達。
- **ストップ**: はい。
- **デフォルト値**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: ATR, SMA, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
