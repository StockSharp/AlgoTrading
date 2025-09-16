# VmMatrix Double Zero

## Overview
VmMatrix Double Zero is a StockSharp port of the MetaTrader 4 expert advisor `vMATRIXDoubleZero`. The original robot looks for "double zero" breakouts by rounding the previous candle close to two decimals and entering trades when price crosses that rounded level. The port keeps the layered filter structure of the EA: configurable multi-bar bias comparisons, optional volume and range checks, an ATR acceleration gate, and a secondary swing-strength filter. The strategy can also require the daily Commodity Channel Index (CCI) to confirm direction and offers an adaptive take-profit component derived from hourly ATR statistics.

Trading is limited to a user-defined terminal-time window, and separate toggles control whether long or short setups may be taken. Stops and targets are managed internally, including an approximation of the original trailing-stop behaviour that widens the take-profit level whenever trailing is enabled.

## Strategy logic
### Bias detection
* **Rounded breakout** – the core trigger compares the close of the last two finished candles with the previous close rounded to two decimals. A long signal requires `Close[2] < round(Close[1], 2)` and `Close[1] > round(Close[1], 2)`; short signals reverse the inequalities.
* **Matrix filter (optional)** – when enabled, six historical candles defined by the parameters `LongK1…LongK6` (for longs) or `ShortK1…ShortK6` (for shorts) are compared using midpoint deviations. Each deviation is calculated as `Close - (High + Low) / 2`. The comparisons mirror the original EA and require the first deviation to dominate the second, the third to exceed a multiplier-scaled fourth (`LongQc`/`ShortQc`), and the fifth to exceed a second multiplier-scaled sixth (`LongQg`/`ShortQg`).

### Additional filters
* **Session filter** – trades are only evaluated when the closing hour of the processed candle falls between `StartHour` and `EndHour` (inclusive).
* **Volume filter** – if enabled, the previous candle’s total volume must exceed `MinimumVolume`.
* **Range compression** – the highest high and lowest low of the last `RangeBars` candles must be within `RangeThresholdPips` pips.
* **ATR acceleration** – compares the most recent ATR value (`AtrPeriod` length on the working timeframe) with the ATR value `AtrShift` bars ago. The signal is accepted only if the current ATR is higher, mimicking the EA’s VSA toggle.
* **Secondary swing filter** – when active, a weighted sum of high/low differences built from the `SecondaryPivot` lookback must be positive for longs or negative for shorts. The weights (`Xb2`, `Xs2`, `Yb2`, `Ys2`) follow the original parameter scheme where 50 represents neutrality.
* **Daily CCI confirmation** – optional gate that requires the most recent daily CCI value (period `DailyCciPeriod`) to be above zero for longs or below zero for shorts.

### Order management
* **Entry size** – orders use `OrderVolume` adjusted to the security’s volume step. If an opposite position is already open, the strategy optionally closes it first (`CloseOnBiasFlip` must be true); otherwise the new entry is skipped because the port runs in a netting environment.
* **Initial stops** – stop-loss distances are expressed in pips through `LongStopLossPips`/`ShortStopLossPips` and converted using the detected pip size. Take-profit distances use `LongTakeProfitPips`/`ShortTakeProfitPips` and may be augmented by the dynamic component below.
* **Dynamic take profit** – when `UseDynamicTakeProfit` is enabled, the strategy adds a weighted combination of hourly ATR statistics and swing differences to the base take-profit distance. The contribution mirrors the EA’s `TPb()` function: it blends the change in hourly ATR(1), the latest hourly ATR(1), hourly ATR(25), and the difference between the highs separated by `SwingPivot` bars. All weights are centred around 50, matching the original interface.
* **Trailing stop** – enabling `UseTrailingStop` activates a step-style trailing stop that raises (or lowers) the stop level whenever price travels roughly twice the configured stop distance beyond the current stop. As in the MQL version, the take-profit distance is multiplied by 10 to effectively keep the trade open while trailing is active.
* **Protective exits** – on each finished candle the strategy checks whether the stop-loss or take-profit has been breached. Positions are closed at market in response. A bias flip (`CloseOnBiasFlip`) also closes the current position if the opposite signal is detected.

## Parameters
The following table summarises the exposed parameters (all are available for optimisation unless noted):

| Group | Parameter | Description |
| --- | --- | --- |
| General | `StartHour` / `EndHour` | Inclusive trading window in terminal time. |
| General | `OrderVolume` | Base order size, normalised to the instrument’s volume step. |
| General | `UseTrailingStop` | Enables the trailing-stop approximation and widens the take-profit factor to emulate the EA. |
| General | `CloseOnBiasFlip` | If true, closes opposing exposure before entering a new trade. |
| Long / Short | `EnableLongs` / `EnableShorts` | Toggles long or short signal processing. |
| Long / Short | `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Stop-loss and take-profit distances measured in pips. |
| Filters | `UseBiasFilter` with `LongK1…LongK6`, `ShortK1…ShortK6`, `LongQc`, `LongQg`, `ShortQc`, `ShortQg` | Configures the matrix-style deviation comparisons for long and short signals. |
| Filters | `UseRangeFilter`, `RangeBars`, `RangeThresholdPips` | Rejects trades when recent price range exceeds the pip threshold. |
| Filters | `UseVolumeFilter`, `MinimumVolume` | Requires the previous candle volume to exceed the threshold. |
| Filters | `UseVsaFilter`, `AtrPeriod`, `AtrShift` | Demands that ATR has increased relative to `AtrShift` bars ago. |
| Filters | `UseSecondaryFilter`, `Xb2`, `Xs2`, `Yb2`, `Ys2`, `SecondaryPivot` | Weighted swing-strength filter based on highs and lows. |
| Filters | `UseDailyCciFilter`, `DailyCciPeriod` | Daily CCI gate; longs need positive CCI, shorts need negative CCI. |
| Take Profit | `UseDynamicTakeProfit`, `WeightSn1…WeightSn4`, `SwingPivot` | Controls the adaptive take-profit component that blends hourly ATR metrics and swing distances. |
| General | `CandleType` | Primary timeframe that drives all signal calculations. |

## Additional notes
* Pip size is inferred from `Security.PriceStep`. Five-digit and three-digit FX symbols are automatically mapped to a 10× multiplier, mirroring the MQL handling of `Digits` and `Point`.
* The port subscribes to three data streams: the working timeframe, hourly candles (for ATR calculations), and daily candles (for CCI). Ensure the data provider can supply all requested timeframes.
* Because StockSharp strategies operate on net positions, hedging the same instrument in both directions simultaneously is not supported. Enable `CloseOnBiasFlip` to mimic the EA’s ability to close and reverse quickly.
* Trailing-stop behaviour is approximate; the EA used raw spread values to determine the trailing step. The port requires price to travel roughly twice the stop distance before advancing the stop, which produces a similar outcome without explicit spread information.
