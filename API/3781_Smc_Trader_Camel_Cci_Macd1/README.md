# SMC Trader Camel CCI MACD Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader 4 expert advisor **"Steve Cartwright Trader Camel CCI MACD"**.
It reproduces the original trading logic based on a camel-style exponential moving average channel,
a MACD trend filter and Commodity Channel Index (CCI) thresholds. Trades are executed on completed
candles to ensure deterministic behaviour and to stay close to the bar-by-bar workflow of the MQL version.

## Trading Logic

1. **Indicators**
   - Two exponential moving averages (EMA) with the same period are applied to candle highs and lows to form the
     camel channel. A breakout of the previous close beyond these envelopes signals momentum strength.
   - A standard MACD indicator (fast EMA, slow EMA and signal line) is used to confirm the underlying trend direction.
   - A CCI indicator validates momentum strength using overbought/oversold levels at ±100 by default.
2. **Long entries**
   - Previous candle close is above the camel high EMA.
   - Previous MACD main value is above zero **and** above the signal line.
   - Previous CCI value is above the positive threshold.
   - No active position is open and no exit occurred within the current candle timeframe (prevents rapid re-entry).
3. **Short entries**
   - Previous candle close is below the camel low EMA.
   - Previous MACD main value is below zero **and** below the signal line.
   - Previous CCI value is below the negative threshold.
   - Same flat-position and cooldown conditions as for long setups.
4. **Exits**
   - Long positions close when the previous MACD main value crosses below the signal line or when the previous CCI
     value drops below the positive threshold.
   - Short positions close when the previous MACD main value crosses above the signal line.
   - After any exit, a cooldown equal to one candle duration is enforced before new entries.

The strategy trades once per bar at most because every decision is based on data from the previous completed candle.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data type/timeframe used for all indicators. | 1-hour time frame |
| `CamelLength` | Length of the high/low EMA channel. | 34 |
| `CciPeriod` | Length of the CCI filter. | 20 |
| `MacdFastPeriod` | Fast EMA length for MACD. | 12 |
| `MacdSlowPeriod` | Slow EMA length for MACD. | 26 |
| `MacdSignalPeriod` | Signal smoothing period for MACD. | 9 |
| `CciThreshold` | Absolute CCI level that must be exceeded for entries (applied symmetrically). | 100 |

All parameters are optimizable through the StockSharp optimizer thanks to the `SetOptimize` calls.

## Risk Management

- Orders are sent via `BuyMarket` and `SellMarket`, inheriting the strategy `Volume` property.
- `StartProtection()` is enabled to initialise standard StockSharp protection helpers.
- No fixed stop-loss or take-profit is defined in the original algorithm; exits rely solely on indicator signals.

## Charting

The strategy automatically plots the camel EMA channel, MACD and CCI indicators, together with own trades,
which replicates the visual cues used in the MT4 implementation.

## Notes

- The cooldown timer uses the candle duration derived from `CandleType.Arg`. Ensure that `CandleType` contains a
  `TimeSpan` argument when you change the timeframe.
- Because all decisions are based on previous-bar values, the order of operations mirrors the `iMACD`, `iCCI`
  and `iMA` (with shift=1) calls in the source EA.
