# MACD Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader expert "Exp_MACDCandle". It converts the color output of a MACD-based candle indicator into trading signals using the StockSharp high-level API.

## Concept

The MACD Candle indicator builds synthetic candles from MACD values calculated on the open and close prices. If the MACD computed on the close is above the MACD computed on the open, the candle is considered bullish (color 2). The opposite yields a bearish candle (color 0). A neutral color (1) appears when both values are equal.

The strategy opens long positions when a bullish candle appears after a non‑bullish one and opens short positions when a bearish candle follows a non‑bearish one. Existing positions are reversed in the new direction.

## Parameters

- `FastLength` – fast EMA period for MACD (default 12).
- `SlowLength` – slow EMA period for MACD (default 26).
- `SignalLength` – signal line period for MACD (default 9).
- `CandleType` – candle type used for calculations, default `TimeFrameCandle` with a four‑hour period.

All parameters are configurable and support optimization.

## Entry and Exit Rules

- **Long entry**: the MACD on the close rises above the MACD on the open while the previous candle was not bullish.
- **Short entry**: the MACD on the open rises above the MACD on the close while the previous candle was not bearish.
- **Exit**: the strategy closes current position when an opposite signal occurs; no explicit stop or take‑profit is applied.

## Notes

- The strategy uses market orders (`BuyMarket` and `SellMarket`).
- Signals are evaluated only on finished candles to avoid noise.
- The example is intended for educational purposes and does not include risk management.
