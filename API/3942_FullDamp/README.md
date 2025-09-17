# Full Damp Strategy

## Overview

The Full Damp strategy is a trend-reversal system built around a triple set of Bollinger Bands combined with a Relative Strength Index (RSI) confirmation filter. The strategy waits for price spikes beyond the widest Bollinger band to detect potential exhaustion. A recent oversold or overbought RSI reading validates the signal before the trade is triggered when price returns inside the medium-width band. Once positioned, exits are managed with partial profit taking, dynamic stop adjustments and Bollinger-based trailing rules.

## Trading Logic

1. **Signal detection**
   * Long setups appear when the candle low closes at or below the lower band of a Bollinger set with width 3. Short setups occur when the candle high reaches the upper band of the same set.
   * The RSI must have reached the oversold (long) or overbought (short) threshold within the last *Lookback Bars* candles. This condition is monitored continuously, so a new RSI extreme refreshes the countdown.
2. **Entry trigger**
   * A long position is opened once price closes back above the lower band of the medium Bollinger set (width 2) provided no position is already open.
   * A short position is opened after price closes below the upper band of the medium Bollinger set.
   * Initial stop-loss levels are anchored to the lowest low (for longs) or highest high (for shorts) seen since the signal candle, expanded by the configurable point offset.
3. **Position management**
   * When the market hits a profit equal to the initial risk, half of the position is closed and the stop-loss is moved to break-even.
   * The remaining volume is exited if the candle high (for longs) or low (for shorts) crosses the medium Bollinger band in the opposite direction.
   * If price returns to the stop level before a profit target is achieved, the whole position is closed.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data source used for analysis and execution. | Hourly candles |
| `BollingerPeriod1` | Period of the narrow Bollinger Bands (width = 1). | 20 |
| `BollingerPeriod2` | Period of the medium Bollinger Bands (width = 2). | 20 |
| `BollingerPeriod3` | Period of the wide Bollinger Bands (width = 3). | 20 |
| `RsiPeriod` | RSI period used for signal confirmation. | 14 |
| `LookbackBars` | Number of completed candles within which the RSI must hit the extreme levels. | 6 |
| `StopOffsetPoints` | Additional buffer (in price points) added to the initial stop-loss level. | 10 |
| `Volume` | Order volume inherited from the base strategy. | 1 |

## Notes

* The RSI thresholds are fixed at 30 for long signals and 70 for short signals to mimic the original MQL logic.
* The strategy uses the high-level StockSharp API: indicators are bound to the candle subscription, trade management uses market orders, and protective logic is handled internally without manual indicator value polling.
* Partial exits and stop adjustments are executed on candle close to keep the behaviour aligned with the original implementation.
