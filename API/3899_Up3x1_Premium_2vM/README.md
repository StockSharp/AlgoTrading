# Up3x1 Premium 2vM Strategy

## Overview

The strategy is a direct port of the MetaTrader 4 expert adviser *up3x1_Premium_2vM*. It trades a single symbol and keeps at most one position open at any time. Entries rely on a combination of smoothed moving averages, strong candle ranges and a daily midnight breakout filter. Risk is managed through fixed take-profit and stop-loss distances expressed in price points, while an optional trailing stop reproduces the behaviour of the original EA that continuously tightens stops once the market moves in favour of the position.

## How it works

1. The primary timeframe is configurable; the EA originally used the chart timeframe. Two smoothed moving averages (SMMA) with periods 12 and 26 are bound to the candle subscription using the typical price.
2. A separate daily candle stream rebuilds the D1 data used by the MQL logic for the midnight breakout filter and for the 10-period daily simple moving average.
3. When flat the strategy evaluates the previous two finished candles and the cached SMMA values:
   - **Long bias**: either the fast SMMA crosses above the slow SMMA while both opens increase, or the last candle shows a bullish body above the configured range thresholds, or the latest daily candle closes bullish after a large range. The original EA also compared the daily SMA to the ask price; because the condition always evaluated to true it is preserved for compatibility.
   - **Short bias**: symmetric conditions of the long rules using bearish ranges and crossovers.
   - If any long condition is satisfied a market buy is issued; otherwise, if any short condition holds a market sell is placed. The requested lot size is normalised to the security volume step before submitting the order.
4. While a position is open the strategy monitors the fast/slow SMMA values from the previous candle. When their absolute difference falls below the `ConvergenceTolerance` the position is closed, reproducing the equality check in the expert adviser.
5. The trailing module tracks the average entry price. Once price travels beyond the trailing distance the stop level is advanced to maintain the configured gap. Touching that level closes the position immediately, emulating the repeated `OrderModify` calls from MQL.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | `TimeFrame(1h)` | Primary timeframe used for entries. |
| `FastMaPeriod` | `12` | Length of the fast smoothed moving average (typical price). |
| `SlowMaPeriod` | `26` | Length of the slow smoothed moving average (typical price). |
| `RangeThreshold` | `0.0060` | Minimum candle range required by the momentum filter. |
| `BodyThreshold` | `0.0050` | Minimum candle body size for the range condition. |
| `DailyRangeThreshold` | `0.0060` | Minimum open-close distance on the latest daily candle for the midnight breakout filter. |
| `TakeProfitPoints` | `150` | Take-profit distance expressed in price points. Set to `0` to disable. |
| `StopLossPoints` | `100` | Stop-loss distance expressed in price points. Set to `0` to disable. |
| `TrailingStopPoints` | `10` | Distance between price and the trailing stop. Set to `0` to disable trailing. |
| `TradeVolume` | `0.05` | Lot size used for market orders before volume normalisation. |
| `ConvergenceTolerance` | `0.00001` | Maximum difference between the SMMAs that triggers position liquidation. |

## Notes

- The strategy keeps the original EA quirk where the daily SMA comparison is always true, guaranteeing feature parity with the MQL source.
- Stop-loss and take-profit orders are registered through `StartProtection` and therefore adapt automatically to the broker step size when available.
- Trailing logic requires both a positive `TrailingStopPoints` value and a valid `Security.PriceStep`. When either piece of information is missing the stop will not trail.
- Volume normalisation honours the exchange constraints (`VolumeStep`, `VolumeMin`, `VolumeMax`). Negative values for `TradeVolume` can be used to emulate percentage-based sizing once custom logic is added.
