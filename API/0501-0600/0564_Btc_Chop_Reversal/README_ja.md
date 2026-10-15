# BTC Chop リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、BTC価格がATRバンドをテストしてモメンタムが転換するときに短期リバーサルを取引します。EMA、ATR、RSI、MACDヒストグラム、出来高スパイクフィルターを組み合わせています。

## 詳細

- **エントリー条件**:
  - **ロング**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` && 売り出来高スパイクなし。
  - **ショート**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ポジションは利益確定とストップロスで保護される。
- **ストップ**: Take profit 0.75%、Stop loss 0.4%。
- **デフォルト値**:
  - `EMA Period` = 23。
  - `ATR Length` = 55。
  - `ATR Multiplier` = 4.4。
  - `RSI Length` = 9。
  - `RSI Overbought` = 68。
  - `RSI Oversold` = 28。
  - `MACD Fast` = 14。
  - `MACD Slow` = 44。
  - `MACD Signal` = 3。
  - `Volume MA Length` = 16。
  - `Sell Spike Multiplier` = 1.5。
  - `Take Profit (%)` = 0.75。
  - `Stop Loss (%)` = 0.4。
- **フィルター**:
  - カテゴリ: リバーサル。
  - 方向: 両方。
  - インジケーター: EMA, ATR, RSI, MACD, 出来高。
  - ストップ: はい。
  - 複雑さ: 中。
  - 時間軸: 短期。
  - 季節性: いいえ。
  - ニューラルネットワーク: いいえ。
  - ダイバージェンス: いいえ。
  - リスクレベル: 中。
