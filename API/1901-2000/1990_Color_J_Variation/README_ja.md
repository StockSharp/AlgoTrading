# Color J バリエーション戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Jurik Moving Averageを使用したColorJVariationエキスパートアドバイザーを再現した戦略です。JMAの傾きを追跡し、方向が変わったときにエントリーします。絶対的なストップロスとテイクプロフィットレベルをサポートします。

## 詳細

- **エントリー条件**:
  - ロング: `PrevSlopeDown && JMA turns up`
  - ショート: `PrevSlopeUp && JMA turns down`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対方向の反転シグナル
- **ストップ**: `StopLoss` と `TakeProfit` による絶対値
- **デフォルト値**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド反転
  - 方向: 両方
  - インジケーター: Jurik Moving Average
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
