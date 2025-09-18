# Expert Candles Strategy

## Overview

The **Expert Candles Strategy** is a StockSharp port of the MetaTrader 5 *Expert_Candles* expert advisor. It monitors the most
recent price action for candlestick reversal formations that feature elongated shadows. Whenever a bullish or bearish composite
candle is detected the strategy opens a position in the respective direction and optionally applies money management identical to
the original EA.

The implementation follows the high-level StockSharp API: candle subscriptions are used to build composite bars, while market
orders and protective levels are managed directly from the strategy.

## Trading logic

1. Each time a candle closes the strategy merges it with up to `Range` previous candles until the full height of the composite
   bar exceeds `MinimumPoints` (converted to price points using the instrument pip size).
2. A **bullish** signal is issued when the composite bar has a shallow upper shadow (`ShadowSmall`) and a deep lower shadow
   (`ShadowBig`). A **bearish** signal is issued when the lower shadow is shallow and the upper shadow is dominant.
3. The entry price is displaced from the candle close by `LimitFactor * rangeSize`. Positive values emulate the original limit
   order that sits inside the candle range.
4. Stop-loss and take-profit targets are positioned at `StopLossFactor` and `TakeProfitFactor` multiples of the composite height.
   If either level is reached on subsequent candles the position is closed immediately.
5. Signals are considered valid for `ExpirationBars` completed candles. Once the time window passes the strategy waits for a new
   formation before submitting fresh orders.
6. Opposite signals close existing positions before initiating trades in the new direction, mimicking the MQL5 behaviour.

## Money management

* `FixedVolume` is used as the default order size.
* When a stop-loss is available and `RiskPercent` is greater than zero, the strategy risks the selected percentage of the
  portfolio equity. The stop distance is converted to monetary value using `Security.PriceStep` and `Security.StepPrice`.
* Volumes are rounded to the instrument `VolumeStep` when the exchange exposes that metadata.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | H1 | Timeframe used to request candles. |
| `Range` | 3 | Maximum number of neighbouring candles combined into a composite pattern. |
| `MinimumPoints` | 50 | Minimal composite height in points (`PriceStep`-based) required to evaluate the pattern. |
| `ShadowBig` | 0.5 | Ratio that the dominant shadow must exceed to confirm the reversal. |
| `ShadowSmall` | 0.2 | Maximum ratio allowed for the opposite shadow. |
| `LimitFactor` | 0.0 | Entry offset as a fraction of the composite height (positive values shift the price inside the candle). |
| `StopLossFactor` | 2.0 | Stop-loss distance as a multiple of the composite height. Set to zero to disable the protective stop. |
| `TakeProfitFactor` | 1.0 | Take-profit distance as a multiple of the composite height. Set to zero to disable the target. |
| `ExpirationBars` | 4 | Number of completed candles during which a signal stays active. |
| `FixedVolume` | 0.1 | Fallback order size used when risk-based sizing cannot be computed. |
| `RiskPercent` | 10 | Percentage of equity risked per trade when a stop-loss is available. |

## Usage notes

- The strategy relies on `Security.PriceStep`, `Security.StepPrice`, and `Security.VolumeStep` to replicate the MetaTrader point
  calculations. Provide accurate instrument metadata or adjust the parameters accordingly.
- Signals are evaluated on closed candles only. Attach the strategy to a time-series connector that emits `CandleStates.Finished`
  events for reliable execution.
- Protective exits are simulated by closing the position as soon as the high or low of a finished candle violates the calculated
  stop-loss or take-profit level.
- The composite candle list is capped at 500 items to keep the memory footprint predictable.

## Differences vs. MetaTrader version

- The StockSharp port uses market orders instead of pending limit orders. The entry offset reproduces the limit behaviour by
  shifting the execution price relative to the candle close.
- Money management is optional; setting `RiskPercent` to zero restores the fixed-lot behaviour from the original EA.
- Stop-loss and take-profit handling is performed inside the strategy rather than by external trailing modules.
