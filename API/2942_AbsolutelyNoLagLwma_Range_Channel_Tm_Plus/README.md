# AbsolutelyNoLagLWMA Range Channel TM Plus Strategy

## Overview
This strategy is a direct port of the MetaTrader expert "Exp_AbsolutelyNoLagLwma_Range_Channel_Tm_Plus". It trades a price channel derived from a double-smoothed linear weighted moving average (LWMA) of the candle highs and lows. The StockSharp version keeps the original behaviour: signals are evaluated on finished candles of a selectable timeframe, the channel state is encoded in the same way as the MQL indicator, and position management follows the same priority order (time exit first, indicator exits second, new entries last).

## Indicator construction
1. For every finished candle the high and low series are pushed into a first LWMA. The length parameter is shared between the high and low streams.
2. The output of the first LWMA is smoothed again with another LWMA of the same length. This recreates the "AbsolutelyNoLagLWMA" smoothing used by the original indicator.
3. The final upper and lower channel values are compared with the candle close:
   * Close above the upper line → bullish breakout state.
   * Close below the lower line → bearish breakout state.
   * Close inside the channel → neutral state.
4. The strategy stores the most recent channel states. The `SignalBar` parameter controls which bar index is checked for signal generation (0 = last closed candle, 1 = one bar back, etc.), matching the `SignalBar` input of the MQL program.

## Signal interpretation
* **Long entry** – enabled by `EnableBuyEntries`. The strategy looks for a bullish breakout on the bar indexed by `SignalBar + 1` while the bar at `SignalBar` has already returned inside the channel. The behaviour replicates the original “previous bar breakout” test.
* **Short entry** – enabled by `EnableSellEntries`. Mirrors the long logic for bearish breakouts.
* **Long exit** – enabled by `EnableBuyExits`. A bearish breakout on the reference bar closes existing long positions, unless they were already closed by the time-based exit on the current candle.
* **Short exit** – enabled by `EnableSellExits`. A bullish breakout on the reference bar closes open shorts, unless the time-based exit already requested the close.

## Trade management
* **Order volume** – taken from the `OrderVolume` parameter. Reversal orders automatically add the absolute value of the current position to avoid residual exposure.
* **Stop loss / Take profit** – optional absolute offsets defined in instrument points (`StopLossPoints`, `TakeProfitPoints`). When positive they are converted to price offsets using the instrument `PriceStep` and passed to `StartProtection`.
* **Time-based exit** – the original EA closes positions that exceed a holding time (`TimeTrade`, `nTime`). In StockSharp this is handled by `UseTimeExit` and `HoldingLimit`. The exit is evaluated before indicator signals on every finished candle.
* **Position timing** – the strategy records the timestamp of the last trade that resulted in a long or short position. These timestamps are used for the time-based exit.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Length` | Length of both LWMA passes that shape the channel. |
| `SignalBar` | Shift of the bar examined for signals (0 = last closed candle). |
| `CandleType` | Timeframe used for the indicator and trade evaluation. |
| `OrderVolume` | Volume used when submitting new entry orders. |
| `StopLossPoints` | Stop-loss distance in instrument points (0 disables the stop). |
| `TakeProfitPoints` | Take-profit distance in instrument points (0 disables the target). |
| `EnableBuyEntries` | Allow or forbid new long positions. |
| `EnableSellEntries` | Allow or forbid new short positions. |
| `EnableBuyExits` | Allow the indicator to close long positions. |
| `EnableSellExits` | Allow the indicator to close short positions. |
| `UseTimeExit` | Enable closing positions after `HoldingLimit` elapses. |
| `HoldingLimit` | Maximum holding time before the time exit is triggered. |

## Notes
* The channel is computed from the candle highs and lows exactly like the bundled MQL indicator `AbsolutelyNoLagLwma_Range_Channel`.
* The strategy ignores unfinished candles and works only with completed data to avoid premature signals.
* Setting `SignalBar` to `0` matches the typical MT5 configuration where the last closed candle is analysed. Higher values reproduce the delayed confirmation used by the default EA (`SignalBar = 1`).
* If `PriceStep` is not available for the selected instrument the stop-loss and take-profit offsets are ignored, preserving the behaviour of zero-valued inputs in the original script.
