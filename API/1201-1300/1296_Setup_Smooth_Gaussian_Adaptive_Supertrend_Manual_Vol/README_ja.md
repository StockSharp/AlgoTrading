# セットアップ: スムースGaussian + Adaptive Supertrend（手動ボラティリティ）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

終値が二重平滑化移動平均線（Gaussian トレンド）を上回ったときにロングエントリーします。
価格がトレンドラインを下回って引けたときにイグジットします。シンプルな手動ボラティリティフィルターでエントリーを制限できます。

## 詳細

- **エントリー条件**: 終値がトレンドラインより上、かつ（ボラティリティフィルター無効またはボラティリティが 2 か 3）。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 終値がトレンドラインを下回る。
- **ストップ**: なし。
- **デフォルト値**:
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: SMA
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
