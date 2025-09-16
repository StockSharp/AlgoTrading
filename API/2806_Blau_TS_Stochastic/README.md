# Blau TS Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader expert advisor "Exp_BlauTSStochastic". The system trades with William Blau's triple-smoothed stochastic oscillator that was bundled with the original MQL package. The indicator computes the highest and lowest prices over a configurable lookback, smooths the stochastic numerator and denominator three times with the selected moving average family, rescales the result to the range [-100, 100], and finally produces a smoothed signal line. All calculations are performed on finished candles that are delivered through the high-level candle subscription API.

The indicator can be built from any of the supported applied prices (close, open, high, low, median, typical, weighted, simple, quarter, two trend-following variants, or DeMark) and four different smoothing algorithms (SMA, EMA, SMMA/RMA, WMA). The `SignalBar` setting allows reproducing the bar shift used by the original expert advisor: the strategy evaluates signals on data that is `SignalBar` bars old, so with the default value of 1 it reacts to the bar that has just closed on the previous step.

## Entry and exit rules

Three trading modes are available. In every mode the boolean toggles `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, and `EnableShortExit` control whether the respective actions are allowed.

### Breakdown mode

*Long entry*: the previous histogram value (shift `SignalBar+1`) is above zero and the more recent value (shift `SignalBar`) is at or below zero. This mirrors the original "histogram breaks through zero" condition and opens or flips a long position while also covering any shorts.

*Short entry*: the previous histogram value is below zero and the more recent value is at or above zero, signalling a zero-line break in the opposite direction. The strategy opens or flips to a short position and optionally closes long exposure.

The same conditions also trigger exits on the opposite side: when the histogram spends the previous bar above zero the strategy closes shorts, and when it spends the previous bar below zero it closes longs.

### Twist mode

*Long entry*: the histogram forms a local bottom. Concretely, the value at shift `SignalBar+1` is below the value at shift `SignalBar+2`, but the value at shift `SignalBar` turns upward and exceeds the intermediate bar. That reproduces the "direction change" mode from the expert advisor.

*Short entry*: the histogram forms a local top. The value at shift `SignalBar+1` is greater than the value at shift `SignalBar+2`, and the most recent value drops below the intermediate bar. Positions in the opposite direction are closed when a twist occurs against them.

### CloudTwist mode

This mode follows the colour changes of the indicator cloud that is defined by the histogram and its signal line.

*Long entry*: the histogram was above the signal line on the previous bar but the most recent value crossed below or touched the signal line. The strategy treats the change of cloud colour as a bullish signal and optionally covers shorts.

*Short entry*: the histogram was below the signal line on the previous bar but the most recent value crossed above or touched the signal line. This flips to a short position and optionally exits longs.

## Risk management

* `StopLossPoints` and `TakeProfitPoints` are measured in instrument price steps. If either value is greater than zero the strategy enables StockSharp's built-in protection block with market orders, so the stops trail the active position automatically.
* The order size is taken from the strategy `Volume` property. When a reversal signal appears the strategy submits `Volume + |Position|` contracts, ensuring that the existing position is closed before opening a new one.

## Parameters

* `CandleType` – time-frame (data type) used for the oscillator (default: 4-hour candles).
* `Mode` – signal detection algorithm: `Breakdown`, `Twist`, or `CloudTwist`.
* `AppliedPrice` – price source for the stochastic calculation (close, open, high, low, median, typical, weighted, simple, quarter, trend-following 0/1, or DeMark).
* `Smoothing` – moving average family used for all smoothing stages (`Simple`, `Exponential`, `Smoothed`, `Weighted`).
* `BaseLength` – number of bars used to compute the high/low range.
* `SmoothLength1`, `SmoothLength2`, `SmoothLength3` – smoothing lengths for the numerator and denominator (applied sequentially).
* `SignalLength` – smoothing length for the histogram signal line.
* `SignalBar` – bar shift that defines which historical values are used for decisions.
* `StopLossPoints`, `TakeProfitPoints` – protective stop and target size in price steps (0 disables the corresponding order).
* `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – permission switches for the four basic actions.

Set the desired `Volume`, attach the strategy to an instrument, and start it. All calculations rely on finished candles, so the system waits until indicators are formed before allowing trades.
