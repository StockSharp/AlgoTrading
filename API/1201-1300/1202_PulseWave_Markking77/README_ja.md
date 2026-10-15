# PulseWave戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VWAP、MACDクロスオーバー、RSIフィルターを使用した戦略。

価格がVWAPを上回り、MACDがシグナルラインを上抜け、RSIが買われすぎ閾値を下回るときに買いエントリーします。価格がVWAPを下回り、MACDがシグナルラインを下抜け、RSIが売られすぎ閾値を上回るときにイグジットします。

## 詳細

- **エントリー条件**: 価格がVWAP以上、MACDが上向きクロスオーバー、RSIが買われすぎ水準未満。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がVWAP未満、MACDが下向きクロスオーバー、RSIが売られすぎ水準超。
- **ストップ**: なし。
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: VWAP, MACD, RSI
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
