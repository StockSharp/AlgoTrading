# RSIトレンドフォロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIトレンドフォロー戦略は、RSI、ストキャスティクス、MACDおよび長期EMAを上回る価格によってモメンタムが確認されたときにロングエントリーします。有利なATRの動きの後にトレーリングストップが有効になり、より短いEMAに追従します。

価格がトレーリングEMAを下回るか、ATRベースのストップロスに達した時点でポジションをクローズします。

## 詳細

- **エントリー条件**: `K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **ロング/ショート**: ロングのみ
- **エグジット条件**: トレーリングEMAを下回るか、ストップロスに到達
- **ストップ**: あり、ATRベース
- **デフォルト値**:
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
