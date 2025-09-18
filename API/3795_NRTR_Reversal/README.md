## Overview
The NRTR Reversal strategy is a StockSharp port of the MetaTrader 4 expert "NRTR_Revers". The original system plots a Noise Reduction Trailing Range (NRTR) line derived from the Average True Range (ATR) and reverses positions whenever price convincingly breaks this adaptive barrier. The StockSharp version keeps the single-position behaviour of the expert advisor, mirrors the ATR-based offset calculation, and manages exits through the built-in protection module.

## Trading logic
1. Subscribe to the main candle series configured by `CandleType` and process finished candles only, replicating the `Bars` counter check from MetaTrader.
2. Feed an `AverageTrueRange` indicator with period `Period`. The most recent ATR value is translated from price units into "points" (price steps) before being multiplied by `AtrMultiplier / 10`, just like the MQL expression `MathRound(k * (iATR / Point) / 10)`.
3. Maintain a rolling cache of recent candles to rebuild the NRTR pivot. The lowest low (for an uptrend) or highest high (for a downtrend) over the last `Period` candles becomes the base pivot.
4. Shift the pivot by the ATR-based offset to form the trailing line:
   - Uptrend: `line = lowestLow - offset`.
   - Downtrend: `line = highestHigh + offset`.
5. Detect a reversal whenever either condition is met:
   - **Close breakout:** the latest candle close crosses the line by more than `offset` points.
   - **Range expansion:** the most recent `Period / 2` candles extend beyond the line by at least `ReverseDistancePoints` points. This reproduces the secondary reversal test from the MQL code that looked further back in history.
6. When the direction flips, send a market order (`BuyMarket` or `SellMarket`) with volume `TradeVolume + |Position|`. This both closes the opposite exposure and opens the new position, matching the MetaTrader behaviour of closing and reversing immediately.
7. Exits are delegated to the risk manager started by `StartProtection`, which converts the configured stop-loss and take-profit distances from points into broker-specific price units.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-minute timeframe | Candle series used for calculations. |
| `TakeProfitPoints` | `decimal` | `4000` | Take-profit distance expressed in instrument price steps. Set to zero to disable. |
| `StopLossPoints` | `decimal` | `4000` | Stop-loss distance in price steps. Set to zero to disable. |
| `TrailingStopPoints` | `decimal` | `0` | Reserved parameter for external trailing modules. Not used inside the strategy. |
| `TradeVolume` | `decimal` | `0.1` | Base volume (lots) mirrored from the MetaTrader setting. |
| `Period` | `int` | `3` | Number of candles used to compute the NRTR pivot. |
| `ReverseDistancePoints` | `int` | `100` | Additional breakout distance in points required for confirmation. |
| `AtrMultiplier` | `decimal` | `3.0` | Multiplier applied to ATR before building the offset. |

## Risk management
- The strategy calls `StartProtection` with `UnitTypes.Step`, so the configured point distances are automatically converted into absolute price offsets based on `Security.PriceStep`.
- If both stop-loss and take-profit are zero, `StartProtection()` is still called to enable StockSharp's position monitoring, replicating the safety checks used by the EA.
- `TrailingStopPoints` is exposed for completeness but left for future extensions, because the original expert did not implement a trailing function despite declaring the parameter.

## Implementation details
- The strategy relies exclusively on the high-level API (`SubscribeCandles().BindEx(...)`) with indicator bindings; no manual indicator loops or prohibited `GetValue` calls are used.
- A compact `CandleSnapshot` struct keeps only high/low/close values from recent candles, avoiding heavy `ICandleMessage` storage while still reproducing the NRTR lookback windows.
- The ATR-to-points conversion honours the MetaTrader formula by dividing the ATR by the instrument step before applying the multiplier and rounding.
- History trimming keeps the cache at `Period * 3` candles to match the original lookback needs without uncontrolled growth.

## Differences from the MetaTrader expert
- Order closing is simplified: instead of iterating through every trade and calling `OrderClose`, the StockSharp port sends a single market order that both flatters the existing position and establishes the new direction.
- Magic numbers, slippage and ticket-specific parameters are omitted because StockSharp manages orders differently.
- Chart annotations are optional; when a chart area is available, the ATR series and own trades are plotted for debugging purposes.

## Usage tips
- Align `TradeVolume` with the exchange lot step (`Security.VolumeStep`) before enabling live trading.
- Tune `Period`, `AtrMultiplier`, and `ReverseDistancePoints` together. Shorter periods require smaller reverse distances to avoid overtrading.
- Set stop/target distances according to the instrument tick size. On instruments with large `PriceStep`, reduce the default 4000-point offsets to realistic levels.

## Indicators
- `AverageTrueRange(Period)` calculated on high/low/close prices.
