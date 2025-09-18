# Above Below MA Strategy

## Overview
The Above Below MA strategy is a StockSharp conversion of the MetaTrader 4 expert advisor "AboveBelowMA". The original script monitors the 15-minute chart of GBP/USD and compares the current price with a one-period exponential moving average (EMA) calculated on the typical price. When price trades on the opposite side of a rising or falling average, the strategy attempts to fade that excursion and rejoin the underlying direction of the EMA. This port keeps the signal structure intact while leveraging StockSharp high-level APIs (`SubscribeCandles` + `Bind`).

## Trading logic
- Subscribe to the configured candle type (15-minute by default) and feed an exponential moving average that uses the typical price `(High + Low + Close) / 3`.
- Track the latest and previous EMA values to understand the short-term slope. A bullish bias requires the EMA to rise, while a bearish bias requires it to fall.
- **Long setup:** when the candle opens at least one price step below the EMA, closes below the EMA, and the previous EMA value is lower than the current EMA value, close any short exposure and prepare to buy. If no position remains, submit a market buy order.
- **Short setup:** when the candle opens at least one price step above the EMA, closes above the EMA, and the previous EMA value is higher than the current EMA value, close any long exposure and prepare to sell. If the position is flat, submit a market sell order.
- Orders are issued only on finished candles to avoid premature signals on partially formed bars.

## Position sizing
- The MetaTrader version sizes trades using `AccountFreeMargin / 10000` capped at 5 lots. The StockSharp implementation offers an equivalent behaviour: when `UseDynamicVolume` is enabled, the strategy divides the current portfolio value by `BalanceToVolumeDivider` (default `10000`).
- The calculated size is limited by `MaxVolume`, mirroring the hard 5-lot cap from the expert advisor. If dynamic sizing is disabled, the `InitialVolume` parameter is used as a fixed volume.
- All volumes are aligned to the instrument's volume step and min/max volume constraints to avoid rejection by the broker or simulator.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `EmaLength` | Period of the exponential moving average (defaults to 1, matching the EA). |
| `CandleType` | Timeframe used to build the candles that feed the EMA (default 15 minutes). |
| `InitialVolume` | Fixed order volume when dynamic sizing is disabled. |
| `UseDynamicVolume` | Enables portfolio-based position sizing (`Balance / BalanceToVolumeDivider`). |
| `BalanceToVolumeDivider` | Divider applied to the portfolio value to emulate `AccountFreeMargin / 10000`. |
| `MaxVolume` | Maximum order volume allowed by the strategy. |

## Notes
- The strategy uses `ClosePosition()` before opening a trade in the opposite direction, matching the MetaTrader logic that closes opposing orders via `CheckOrders`.
- Because signals are evaluated on finished candles, entries may occur slightly later than the tick-based MetaTrader version. This change improves stability when running in backtests or live trading with candle data.
- Ensure that the selected security provides meaningful `PriceStep`, `VolumeStep`, and portfolio valuation information for the dynamic volume block to work as expected.
