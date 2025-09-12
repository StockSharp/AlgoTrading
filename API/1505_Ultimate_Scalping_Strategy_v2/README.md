# Ultimate Scalping Strategy v2
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping system combining fast and slow EMAs with VWAP. Optional engulfing candle and volume spike filters refine entries. Positions use ATR-based stops and can close on opposite signals.

## Details

- **Long**: fast EMA crosses above slow EMA and price above VWAP.
- **Short**: fast EMA crosses below slow EMA and price below VWAP.
- **Indicators**: EMA, VWAP, ATR, SMA.
- **Stops**: ATR multiples.
