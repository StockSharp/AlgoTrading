# My TS15 Moving Average Trailing Stop

## Overview

This strategy reproduces the behaviour of the original **my_ts15.mq5** expert advisor by managing trailing stop orders around an existing net position. A linear weighted moving average (LWMA) drives the stop placement and can be replaced by other smoothing methods. The logic continuously:

* Reads the moving average value from a configurable number of completed candles.
* Compares price progress with the moving average trail and price-based offsets.
* Moves the protective stop order only when the new level improves the previous one by at least the specified step.
* Optionally enforces a maximum loss distance by clamping the stop or immediately liquidating the position when the limit is broken.

The strategy does not produce entry signals. It is meant to run together with other components (manual or automated) that open positions on the same security.

## Trading Logic

1. Subscribe to the selected candle series and bind a moving average indicator using the StockSharp high-level API.
2. As soon as a candle is finished, store the indicator result and obtain the value that is `MaBarsTrail + MaShift` bars behind the current bar.
3. Convert the point-based settings to absolute price distances using the instrument tick size.
4. For long positions, choose the lowest of:
   * The moving average minus its offset.
   * The current price minus the “in profit” offset.
   Afterwards clamp the trail to the “in loss” distance and optionally to the maximum allowed loss.
5. For short positions, choose the highest of:
   * The moving average plus its offset.
   * The current price plus the “in profit” offset.
   Afterwards clamp the trail to the “in loss” distance and optionally to the maximum allowed loss.
6. Update the stop order only when the improvement exceeds `TrailStepPoints` (unless it is zero, in which case every improvement is accepted).
7. If the price breaches the maximum loss distance and `EnforceMaxStopLoss` is enabled, the strategy closes the position immediately.

All price inputs use the candle price specified in `MaPrice`, matching the original MQL setting where the indicator is fed with the `PRICE_WEIGHTED` series.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `MaPeriod` | `50` | Length of the moving average used as the trailing backbone. |
| `MaShift` | `0` | Additional shift (in bars) applied when sampling the moving average value. |
| `MaMethod` | `LinearWeighted` | Smoothing method of the moving average (simple, exponential, smoothed, linear weighted). |
| `MaPrice` | `Weighted` | Candle price fed to the moving average. |
| `MaBarsTrail` | `1` | Number of completed bars between the current candle and the moving average sample. |
| `TrailBehindMaPoints` | `5` | Distance in points kept between the stop and the moving average. |
| `TrailBehindPricePoints` | `30` | Distance in points kept behind the price when the position is profitable. |
| `TrailBehindNegativePoints` | `60` | Distance in points kept behind the price when the position is losing. |
| `TrailStepPoints` | `0` | Minimum improvement (in points) required before moving the stop. Zero replicates the “always update” behaviour. |
| `EnforceMaxStopLoss` | `false` | If enabled, clamp the stop to the maximum allowed loss and liquidate the position when price exceeds that limit. |
| `MaxStopLossPoints` | `100` | Maximum allowed loss distance in points. |
| `ShowIndicator` | `true` | Draw the moving average and the trade markers on the chart when the UI is available. |
| `CandleType` | `M1` | Candle data type driving the calculations. |

All point-based inputs are converted to price distances via the instrument pip size calculated from `Security.PriceStep`.

## Conversion Notes

* The MQL expert refreshed the MA handle manually. The StockSharp implementation uses `BindEx` to process the indicator without accessing internal buffers or calling `GetValue`.
* Bid/Ask prices are not directly available from finished candles, therefore the trailing calculations use the candle price selected by `MaPrice`. This keeps the behaviour consistent because the original script fed the indicator with the same weighted price and compared it with Bid/Ask ticks.
* `PositionModify` is replaced by cancelling and recreating protective stop orders (`SellStop` for long, `BuyStop` for short). The strategy stores the last stop level to mimic the MetaTrader trailing thresholds.
* The optional forced close (`pre_init`) follows the original logic: once the market moves beyond `MaxStopLossPoints`, the position is closed immediately.
* No entry logic has been added; users should combine this trailing module with their own signal provider.

## Usage Tips

1. Attach the strategy to the same security that opens the positions.
2. Adjust the point distances to the instrument tick size (Forex symbols generally use “pip” values, CFDs may require different multipliers).
3. Set `TrailStepPoints` to a positive value to reduce order churn on illiquid instruments.
4. Disable `EnforceMaxStopLoss` if another risk manager already controls hard stop distances.
5. Keep `ShowIndicator` enabled while tuning the parameters to visualise the moving average and trailing behaviour.
