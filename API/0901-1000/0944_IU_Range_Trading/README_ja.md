# IU レンジトレーディング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

IU Range Trading戦略は、ルックバック期間にわたる価格レンジがATR乗数の範囲内に収まる統合ゾーンを識別します。価格がレンジの境界を超えるとブレイクアウト取引がトリガーされます。ポジションは有利な価格アクションに追随するATRベースのトレーリングストップで保護されます。

## 詳細

- **エントリー条件**: 価格がATRで定義された狭いレンジを上下にブレイクアウト。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRベースのトレーリングストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RangeLength` = 10
  - `AtrLength` = 14
  - `AtrTargetFactor` = 2.0m
  - `AtrRangeFactor` = 1.75m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: ATR, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
