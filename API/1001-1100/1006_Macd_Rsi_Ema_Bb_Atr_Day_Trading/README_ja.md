# MACD RSI EMA BB ATRデイトレード戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACDシグナルクロス、RSI境界、EMAトレンド方向をボリンジャーバンドのスクイーズフィルターと組み合わせたイントラデイ戦略です。リスク管理にはATRベースのストップロス、トレーリングストップ、リスクリワード利確を使用します。

## 詳細

- **エントリー条件**: トレンド方向にMACDがシグナルをクロス、RSIが閾値内、BBスクイーズなし。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のストップまたは目標値。
- **ストップ**: ATRベースのストップロス、トレーリングストップ、リスクリワード利確。
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD, RSI, EMA, Bollinger Bands, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
