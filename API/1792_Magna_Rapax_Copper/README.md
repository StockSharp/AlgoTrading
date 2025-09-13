# Magna Rapax Copper Strategy

This strategy replicates the "rainbow" moving average system from the original MQL expert.
It uses eleven exponential moving averages together with MACD and ADX filters.

## How it works

- Calculate EMA(2), EMA(3), EMA(5), EMA(8), EMA(13), EMA(21), EMA(34), EMA(55), EMA(89), EMA(144) and EMA(233) on close prices.
- Calculate MACD (Fast, Slow, Signal) and use the signal line.
- Calculate ADX to measure trend strength.
- **Buy** when:
  - MACD signal line is above zero.
  - All EMAs are strictly ascending (each faster EMA above the slower one).
  - ADX value is above the threshold.
- **Sell** when:
  - MACD signal line is below zero.
  - All EMAs are strictly descending.
  - ADX value is above the threshold.

Positions are reversed when an opposite signal appears.

## Parameters

| Name | Description |
| --- | --- |
| `FastMacd` | Fast EMA period for MACD. |
| `SlowMacd` | Slow EMA period for MACD. |
| `SignalPeriod` | Signal line period for MACD. |
| `AdxPeriod` | Period for ADX indicator. |
| `AdxThreshold` | Minimum ADX value required to trade. |
| `CandleType` | Candle timeframe used for calculations. |

## Notes

- Strategy uses market orders via `BuyMarket` and `SellMarket`.
- Only one position is kept at a time; an opposite signal reverses the position.
- This is a direct conversion of the original MQL strategy without the optional martingale logic.
