# Hpcs Inter6 RSI Strategy

## Overview
Hpcs Inter6 RSI Strategy ports the MetaTrader expert `_HPCS_Inter6_MT4_EA_V01_WE` to the StockSharp high-level API. The algorithm watches the Relative Strength Index (RSI) on a configurable candle series and reacts to fast reversals around the classical 70/30 thresholds. Whenever RSI crosses above 70 the strategy flips into a short position, while a cross below 30 flips into a long position. Each trade immediately attaches symmetric take-profit and stop-loss levels measured in pips.

## Data and indicators
- **Candle source** – user-selected time frame (default 1 hour).
- **Indicator** – Relative Strength Index with configurable length (default 14). The indicator is recalculated through the StockSharp indicator binding pipeline.

## Entry logic
1. The strategy waits for a finished candle to avoid trading on incomplete data.
2. On every completed candle it compares the new RSI value with the previous value.
3. **Short setup:** if RSI has just crossed above `UpperLevel` (default 70) from below, the strategy sells using a market order. Existing long exposure is closed before the short is established so the resulting net position is short by exactly the configured volume.
4. **Long setup:** if RSI has just crossed below `LowerLevel` (default 30) from above, the strategy buys using a market order. Existing shorts are covered first so the net position becomes long by the configured volume.
5. Only one entry per candle is allowed. Multiple signals inside the same bar are ignored to mirror the MetaTrader implementation that uses the bar timestamp guard.

## Exit logic
- Every entry defines a fixed target and stop at the same distance measured in pips.
- While in a long position the strategy exits if the candle high touches the target or if the low touches the protective stop.
- While in a short position the strategy covers if the candle low reaches the target or if the high reaches the protective stop.
- When the position is flat all protective levels are cleared.

The pip distance is translated into price units using the instrument tick size. For instruments with three or five decimal places the algorithm multiplies the distance by ten to match the MetaTrader notion of one pip.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1-hour time frame | Time frame that feeds the RSI indicator. |
| `RsiLength` | 14 | Lookback period of the RSI. |
| `UpperLevel` | 70 | RSI level that triggers short entries when crossed from below. |
| `LowerLevel` | 30 | RSI level that triggers long entries when crossed from above. |
| `TradeVolume` | 1 | Order size for market entries. Existing exposure is closed before reversing. |
| `OffsetInPips` | 10 | Distance of both take-profit and stop-loss from the entry price, expressed in pips. |

All parameters are exposed through `StrategyParam` objects so they can be optimized inside StockSharp.

## Notes
- The strategy relies on candle high/low to simulate take-profit and stop-loss fills, matching the behavior of fixed-price targets in MetaTrader.
- No pending orders are placed; all executions are market orders handled by the strategy core.
- The indicator and chart bindings are automatically created when a chart area is available, providing a visual overlay of candles and RSI.
