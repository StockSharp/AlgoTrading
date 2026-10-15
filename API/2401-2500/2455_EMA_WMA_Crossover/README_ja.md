# EMA WMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足の始値で計算された指数移動平均（EMA）と加重移動平均（WMA）のクロスオーバーに基づく戦略です。
EMAがWMAを下抜けしたときにロング、EMAがWMAを上抜けしたときにショートに入ります。
ポジションサイズは口座資産のリスクパーセントで決定されます。
戦略はティック単位で定義された固定のテイクプロフィットとストップロス距離を使用します。

## 詳細

- **エントリー条件**:
  - ロング: `EMA crosses below WMA`
  - ショート: `EMA crosses above WMA`
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: あり
- **デフォルト値**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: 移動平均クロスオーバー
  - 方向: 両方
  - インジケーター: EMA, WMA
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
