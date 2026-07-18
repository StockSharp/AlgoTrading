# Tri-Monthly BTC スイング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Tri-Monthly BTC SwingはEMA200、MACDクロスオーバー、RSIフィルターを使用してトレードします。
この戦略は90日ごとに1回のみトレードを許可します。

## 詳細

- **エントリー条件**: EMA200を上回る終値、シグナルを上回るMACD線、閾値を上回るRSI、かつ前回トレードから少なくとも90日経過
- **ロング/ショート**: ロング
- **エグジット条件**: MACD線がシグナルを下回るまたはRSIが閾値を下回る
- **ストップ**: なし
- **デフォルト値**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90日
  - `CandleType` = 1日
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: EMA, MACD, RSI
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
