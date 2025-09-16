
# Exp Trading Channel Index Strategy

## Overview
This strategy is a StockSharp port of the MQL5 expert advisor `Exp_Trading_Channel_Index`. It follows the Trading Channel Index (TCI) oscillator, a volatility-adjusted momentum indicator that colors each bar according to its position relative to two channel levels. The strategy reacts when the color assigned to a historical bar changes, mimicking the original expert advisor behaviour.

The implementation subscribes to a configurable candle series (default: H4) and processes only finished candles. All trade management decisions are taken on the open of the next bar after a color change, just as in the source script.

## Trading Channel Index indicator
The TCI is calculated through three stages:

1. **Primary smoothing** of the chosen price source via a configurable moving average (SMA, EMA, SMMA, WMA, or Jurik). This produces the baseline `XMA` value.
2. **Volatility estimation** by smoothing the absolute deviation between price and the baseline.
3. **Normalization** of the deviation by the configured coefficient and a second smoothing stage. The resulting value is compared with the `HighLevel` and `LowLevel` thresholds to assign one of five color codes:
   - `0` (lime) – value is above `HighLevel`.
   - `1` (teal) – value is positive but below `HighLevel`.
   - `2` (gray) – value is near zero.
   - `3` (orange) – value is negative but above `LowLevel`.
   - `4` (gold) – value is below `LowLevel`.

The StockSharp version uses native indicator classes for the moving averages. Jurik MA honours the `Phase` input while other methods ignore it, matching the original behaviour where the phase parameter is only meaningful for JJMA.

## Entry and exit rules
The algorithm inspects the bar specified by `SignalBar` (default 1, i.e. the last closed candle) and the bar before it:

- **Open long**: two bars ago (`SignalBar + 1`) had color `0` (extreme positive) and the last bar (`SignalBar`) has a different color. A short position is closed first if it exists, then a new long of `TradeVolume` lots is opened.
- **Open short**: two bars ago had color `4` (extreme negative) and the last bar has a different color. A long position is closed first if it exists, then a new short is opened.
- **Close long**: whenever the older bar (two bars ago) is colored `4`, signalling bearish exhaustion.
- **Close short**: whenever the older bar is colored `0`, signalling bullish exhaustion.

The logic reproduces the flag-based management of `TradeAlgorithms.mqh`: exits are evaluated before entries, and opposite trades are flattened before opening a new position.

## Risk management
Optional protective orders are implemented in price-step units:

- `StopLossPoints` defines the distance between the entry price and the stop-loss level. The stop is placed below long entries and above short entries.
- `TakeProfitPoints` defines the profit target distance using the same step-based measure.

Stops are checked on every finished candle. If both stop and target would trigger on the same bar the first condition that becomes true closes the position.

## Parameters
- **Trade Volume** (`TradeVolume`): order quantity for each new position.
- **Stop Loss (pts)** (`StopLossPoints`): stop-loss distance in price steps.
- **Take Profit (pts)** (`TakeProfitPoints`): take-profit distance in price steps.
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`): toggles for long signals.
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`): toggles for short signals.
- **Signal Bar** (`SignalBar`): how many bars back to evaluate for the color change.
- **High Level / Low Level** (`HighLevel`, `LowLevel`): thresholds for color assignment.
- **Primary / Secondary Method** (`Method1`, `Method2`): moving average types for both smoothing stages.
- **Length #1 / Length #2** (`Length1`, `Length2`): periods used by the moving averages.
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`): Jurik phase settings (ignored by other methods).
- **Coefficient** (`Coefficient`): normalisation factor applied to the deviation.
- **Applied Price** (`AppliedPrice`): price source (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, trend-follow average, Demark).
- **Candle Type** (`CandleType`): timeframe used for indicator calculations.

## Notes
- Python port is intentionally omitted as requested.
- The StockSharp version keeps the tab-based indentation guideline and adds English comments throughout the code.
- The indicator does not draw colour histograms; however, both the numeric value and the colour index are available via the custom `TradingChannelIndexValue` class for further visualisation if desired.
