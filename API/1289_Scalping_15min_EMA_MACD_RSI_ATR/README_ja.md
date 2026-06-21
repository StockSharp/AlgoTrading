# スキャルピング戦略 15m EMA MACD RSI ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

50期間 EMA トレンドフィルター、MACD ヒストグラムのモメンタム、RSI レベルを組み合わせたスキャルピング戦略です。リスク管理には ATR ベースのストップロスとテイクプロフィットを使用します。

価格が EMA を上回り、MACD ヒストグラムが正で、RSI が 50 と買われすぎレベルの間にある場合に買いエントリーします。価格が EMA を下回り、ヒストグラムが負で、RSI が売られすぎレベルと 50 の間にある場合にショートエントリーします。ストップと目標値は終値から ATR の倍数で追従します。

## 詳細

- **エントリー条件**: EMA に対する価格の位置、MACD ヒストグラムの符号、RSI レベル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATR ベースのストップロスまたはテイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `EmaPeriod` = 50
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `AtrPeriod` = 14
  - `SlAtrMultiplier` = 1m
  - `TpAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: EMA, MACD, RSI, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
