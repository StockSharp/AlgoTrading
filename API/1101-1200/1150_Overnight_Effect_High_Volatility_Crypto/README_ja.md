# 高ボラティリティ暗号資産のオーバーナイト効果戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高ボラティリティの夜間にロングポジションを建て、深夜前にクローズする戦略です。ボラティリティは設定可能な期間の対数リターンの標準偏差によって測定され、過去のボラティリティの中央値と比較されます。

## 詳細

- **エントリー条件**:
  - `UseVolatilityFilter` が有効の場合: `currentHour == EntryHour && highVolatility`
  - フィルター無効の場合: `currentHour == EntryHour`
- **ロング/ショート**: ロング
- **ストップ**: なし
- **デフォルト値**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: 時間ベース
  - 方向: ロングのみ
  - インジケーター: StandardDeviation, Median
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
