# RSI 自動化戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

相対力指数（RSI）を使用して、極端な売られすぎと買われすぎの状態で取引するモメンタム戦略です。
RSIが売られすぎレベルを下回るとロングポジションを開き、RSIが買われすぎレベルを上回るとショートポジションを開きます。
RSIが中間の閾値に戻るか、ストップロス、テイクプロフィット、またはトレーリングストップレベルが発動されたときにポジションをクローズします。

## 詳細

- **エントリー条件**: ロングの場合はRSIが `Oversold` を下抜け、ショートの場合はRSIが `Overbought` を上抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: RSIが `ExitLevel` を横断、ストップロス、テイクプロフィット、またはトレーリングストップ。
- **ストップ**: はい、固定ストップロス、テイクプロフィット、オプションのトレーリングストップ。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
