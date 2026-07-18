# Ichimoku RSI MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ichimokuクラウド、RSI、MACDクロスオーバーシグナルを組み合わせたトレンドフォロー戦略。

## 詳細

- **エントリー条件**: Ichimokuクラウドの上/下に価格があり、RSIフィルターを満たし、MACDラインがシグナルラインをクロスする。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対方向のMACDクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Ichimoku, RSI, MACD
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
