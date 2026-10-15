# 昨日の高値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

前日高値の上に買いストップ注文を置くロングブレイクアウト戦略です。
オプションの ROC フィルター、トレーリングストップ、EMA でのクローズが追加のリスク管理を提供します。

## 詳細

- **エントリー条件**: 昨日の高値を下回る終値の後、高値 + gap で buy stop
- **ロング/ショート**: ロングのみ
- **エグジット条件**: ストップロス、テイクプロフィット、オプションのトレーリングストップまたは EMA クロス
- **ストップ**: あり、パーセンテージベース
- **デフォルト値**:
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 minute
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロング
  - インジケーター: Price, ROC, EMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
