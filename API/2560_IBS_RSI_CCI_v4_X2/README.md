# IBS RSI CCI v4 X2 Strategy

## Overview

The **IBS RSI CCI v4 X2 Strategy** is a multi-timeframe momentum system that blends the Internal Bar Strength (IBS), Relative Strength Index (RSI), and Commodity Channel Index (CCI). The original algorithm from the MetaTrader 5 ecosystem has been ported to StockSharp and redesigned to use high level candle subscriptions with indicator bindings. Two independent indicator pipelines are evaluated: a slow "trend" timeframe that defines the directional bias and a fast "signal" timeframe that generates entry and exit decisions.

On each timeframe the strategy computes a composite oscillator. The oscillator value is derived from the weighted contributions of IBS, RSI and CCI. Rapid changes in the composite value are smoothed, clamped by a configurable momentum threshold and wrapped with a volatility envelope that mimics the original indicator buffering logic. Crossovers between the composite value and its smoothed envelope are the core triggers used for decisions.

### Trading logic

1. **Trend detection** – The slow timeframe monitors the composite oscillator. If the composite stays above the envelope the strategy marks an uptrend, otherwise it flags a downtrend.
2. **Signal generation** – The fast timeframe evaluates two consecutive values of the composite and envelope. Crossovers on the newest bar confirm an actionable signal only when the previous bar supports the transition.
3. **Entry rules** –
   * Enter long only when long trades are allowed, the current trend is bullish and the composite crosses below the envelope on the fast timeframe (bearish-to-bullish reversal in the original indicator orientation).
   * Enter short only when short trades are allowed, the current trend is bearish and the composite crosses above the envelope on the fast timeframe.
4. **Exit rules** –
   * Optional immediate exits on composite crossovers when the `_CloseLongOnSignalCross` or `_CloseShortOnSignalCross` toggles are enabled.
   * Forced trend-based exits when `_CloseLongOnTrendFlip` or `_CloseShortOnTrendFlip` request closing as soon as the slow timeframe bias reverses.
   * Risk management is handled through StockSharp `StartProtection`, translating the configured point-based stop loss and take profit distances into absolute price offsets using the instrument price step.

### Indicators and calculations

* **Internal Bar Strength (IBS):** `(close - low) / max(high - low, price step)` smoothed by a selectable moving average.
* **RSI:** Standard RSI applied to a configurable applied price (close, open, high, low, median, typical or weighted).
* **CCI:** Custom CCI implementation with a simple moving average and mean deviation estimator derived from the selected applied price.
* **Composite oscillator:** Weighted sum of the transformed IBS, RSI and CCI values divided by three, clamped by the `Threshold` setting to replicate the original "momentum limiter".
* **Envelope:** Highest and lowest composite readings over the configured range are smoothed twice and averaged to produce the signal baseline used for crossovers.

The implementation avoids direct indicator value polling (`GetValue`) by keeping all state inside the calculator classes and by feeding candles sequentially through the high level API.

## Parameters

| Parameter | Description |
| --- | --- |
| `OrderVolume` | Base order size used when opening a new position. |
| `TrendCandleType` | Candle type for the slow timeframe subscription. |
| `TrendIbsPeriod`, `TrendIbsMaType` | IBS smoothing period and moving average type for the slow timeframe. |
| `TrendRsiPeriod`, `TrendRsiPrice` | RSI period and applied price for the slow timeframe. |
| `TrendCciPeriod`, `TrendCciPrice` | CCI period and applied price for the slow timeframe. |
| `TrendThreshold` | Momentum clamp threshold used in the slow timeframe composite. |
| `TrendRangePeriod`, `TrendSmoothPeriod` | Look-back range and smoothing window for the slow timeframe envelope. |
| `TrendSignalBar` | Offset (number of closed candles back) used when reading slow timeframe values. |
| `AllowLongEntries`, `AllowShortEntries` | Enable or disable new long/short trades. |
| `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip` | Force position exits when the slow timeframe bias turns opposite. |
| `SignalCandleType` | Candle type for the fast timeframe subscription. |
| `SignalIbsPeriod`, `SignalIbsMaType` | IBS smoothing configuration for the fast timeframe. |
| `SignalRsiPeriod`, `SignalRsiPrice` | RSI settings for the fast timeframe. |
| `SignalCciPeriod`, `SignalCciPrice` | CCI settings for the fast timeframe. |
| `SignalThreshold` | Momentum clamp threshold used in the fast timeframe composite. |
| `SignalRangePeriod`, `SignalSmoothPeriod` | Envelope range and smoothing on the fast timeframe. |
| `SignalSignalBar` | Offset applied when evaluating fast timeframe signals. |
| `CloseLongOnSignalCross`, `CloseShortOnSignalCross` | Optional exit triggers on fast timeframe crossovers. |
| `StopLossPoints`, `TakeProfitPoints` | Stop loss and take profit distances measured in price step points. |

## Usage notes

1. Configure the security and candle types before starting the strategy. Both timeframes will be subscribed automatically through `GetWorkingSecurities`.
2. The default configuration mirrors the original MQL version: 8-hour trend candles with 1-hour signal candles and identical indicator settings on both timeframes.
3. Because the composite oscillator is internally clamped, extreme volatility periods may produce flatter responses than typical momentum strategies. Adjust the `Threshold`, `RangePeriod` and `SmoothPeriod` parameters to adapt sensitivity.
4. The built-in position protection relies on the instrument `PriceStep`. Ensure the security metadata provides a valid step, otherwise consider adjusting the fallback in code.
5. Use StockSharp charting helpers if you need to visualise the behaviour. The strategy already draws the signal timeframe candles and executed trades when a chart area is available.

## Risks and limitations

* The strategy assumes sequential candle delivery. Out-of-order candle updates may desynchronise the internal buffers.
* Mean deviation in the custom CCI is recalculated from the buffered values; the accuracy depends on receiving a continuous data stream without gaps.
* When `OrderVolume` is combined with existing exposure, flips will be performed by sending a single market order sized to close the opposite position and open the new one. Ensure the brokerage permissions allow that behaviour.
* The port preserves the orientation of the original indicator (negative coefficients). Signals may therefore appear counter-intuitive until you review the legacy indicator design.

## Extending the strategy

* Tune moving average types independently for the envelope and the IBS smoothing to explore faster or slower reactions.
* Replace the custom CCI calculator with StockSharp’s built-in indicator if a future release exposes the necessary price selectors.
* Add chart overlays by binding the composite values to additional chart panes when more visual feedback is required.
* Combine with additional risk controls such as maximum daily loss or trade time filters for production deployments.
