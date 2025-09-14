# Ergodic Ticks Volume OSMA Strategy

## Overview
This strategy adapts the MQL5 expert "Exp_Ergodic_Ticks_Volume_OSMA" to StockSharp. The original expert uses a custom indicator to evaluate tick volume momentum. In this version the custom indicator is approximated by the MACD histogram.

The strategy looks for consecutive increases or decreases in the histogram:
- Two rising steps trigger a long entry and close any short position.
- Two falling steps trigger a short entry and close any long position.

`StartProtection()` is used to avoid conflicts with existing positions when the strategy starts.

## Parameters
- `FastLength` – fast EMA period for the MACD. Default: 12.
- `SlowLength` – slow EMA period for the MACD. Default: 26.
- `SignalLength` – signal EMA period for the MACD. Default: 9.
- `CandleType` – timeframe of candles, default is 8 hours.

## Trading Logic
1. Subscribe to candles of the selected `CandleType`.
2. Compute the MACD histogram for each finished candle.
3. If the histogram grows for two consecutive bars, close shorts and buy.
4. If the histogram falls for two consecutive bars, close longs and sell.
5. Continue processing for each new candle.
