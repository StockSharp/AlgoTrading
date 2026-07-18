# 変化率（Rate of Change）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRate of Changeインジケーターを使用してバブル状態を検出し、ゼロラインクロスオーバーを動的なポジションサイジングで取引します。

主要資産の日次データでバックテストにより安定したパフォーマンスが示されています。

## 詳細

- **エントリー条件**: ROCがゼロを上下に交差する；バブル崩壊時のオプションショート。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: RateOfChange
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
