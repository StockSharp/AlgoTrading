# Exp iCustom Strategy

## Overview

`ExpICustomStrategy` is a StockSharp port of the classic MetaTrader expert advisor that executes trades based on buffers produced by any custom indicator. The conversion keeps the modular configuration style of the original robot while embracing the high-level StockSharp API. All trading rules are driven by indicator buffers, so the same strategy class can reproduce many different behaviours depending on which indicator you choose.

* Single entry/exit engine controlled by indicator buffers and adjustable interpretation modes.
* Fully configurable indicator creation: set the type name and pass parameters as a slash separated list (`Length=14/Width=2`).
* Risk management features from the MQL version are preserved: fixed stop-loss/take-profit, break-even, classical trailing stop and trailing by a secondary indicator.
* Operational limits mirror the expert advisor: sleep bars between entries, per-side maximum order counters and profit/stop filters for discretionary exits.
* Only market execution is implemented; pending order logic from the MQL file is deliberately omitted to stay within the recommended high-level API.

## Indicator configuration

The strategy instantiates indicators by reflection. Set `EntryIndicatorName`, `CloseIndicatorName` and `TrailingIndicatorName` to the desired type. You can use either a fully qualified type name (`StockSharp.Algo.Indicators.SMA`) or just the class name (`SMA`). Parameters are supplied through strings such as:

```
Length=20/Width=2
```

Each segment is separated by `/`.

* If the segment contains an `=` sign the text before it is treated as the property name (case insensitive) and the value is parsed using invariant culture.
* If the segment does not contain `=` the value is assigned to the first writable numeric/bool property that has not been configured yet.
* Supported target types are `int`, `decimal`, `double`, `bool` and enumerations (numeric or textual values).

Trailing indicator parameters follow the same rules. Leave the string empty when no separate indicator is required.

## Entry logic

`EntryMode` replicates the `_O_Mode` switch from the expert advisor.

1. **Arrows** – interprets buffers as binary signals. `EntryBuyBufferIndex` and `EntrySellBufferIndex` must point to buffers that emit non-zero values on entry bars. `EntryShift` controls which historical bar is read (default `1` = last finished bar).
2. **Cross** – compares `EntryMainBufferIndex` and `EntrySignalBufferIndex`. A long entry triggers when the main buffer moves above the signal buffer on the most recent bar while it was not above on the previous bar (`shift + 1`). The short rule mirrors the logic.
3. **Levels** – uses `EntryMainBufferIndex` against thresholds. Long entries require the value to pierce above `EntryBuyLevel`; shorts trigger when the buffer falls below `EntrySellLevel`.
4. **Slope** – checks slope reversal. The most recent value must exceed the previous bar (`shift + 1`) and the value two bars back (`shift + 2`) for a long signal; short logic is symmetric.
5. **RangeBreak** – looks for buffers that emit a positive/negative marker once. A new long signal occurs when the buy buffer becomes non-zero while the previous bar was empty or non-positive.

When both buy and sell conditions fire on the same bar, signals cancel each other to avoid ambiguous actions.

## Exit logic

`CloseMode` uses the same five interpretation modes and is evaluated on either the dedicated close indicator or the entry indicator (when `CloseUseOpenIndicator=true`). Additional filters:

* `CheckProfit` forces an exit only when profit exceeds `MinimalProfit` points from the entry price.
* `CheckStopDistance` skips discretionary exits if the active stop-loss is farther than `MinimalStopDistance` points from the entry (prevents premature exits when a protective stop already covers profit).

If `EntryMode` is `Cross`, the reversal of the entry signal is reused automatically (behaviour of `_O_Mode = 2` from MetaTrader).

## Risk management and trailing

* **Fixed stop-loss / take-profit** – `StopLossPoints` and `TakeProfitPoints` are converted to absolute prices using the security price step. Both values can be zero to disable the feature.
* **Classic trailing stop** – activate with `TrailingStopEnabled`. Once unrealised profit reaches `TrailingStartPoints`, the stop follows the price at a distance of `TrailingDistancePoints`.
* **Break-even** – enable with `BreakEvenEnabled`. After the price advances by `BreakEvenStartPoints`, the stop is moved to lock in `BreakEvenLockPoints`.
* **Indicator trailing** – when `IndicatorTrailingEnabled` is true, the trailing indicator is evaluated on every bar. For long positions the trailing buffer value is reduced by `TrailingIndentPoints` and must stay above the entry price plus `TrailingProfitLockPoints`. The stop is raised only when the indicator proposes a higher level than the current stop. Short positions use the symmetric logic.

Stops created by the different modules cooperate: the most protective (tightest) stop wins for short positions, and the highest stop wins for long positions.

## Order handling and limits

* `SleepBars` implements the original “sleep mode”: after opening a position on a bar the strategy waits for the specified number of bars before accepting another signal in the same direction. `CancelSleeping` resets the counter when the opposite trade executes.
* `MaxOrdersCount`, `MaxBuyCount` and `MaxSellCount` restrict the number of stacked trades. The StockSharp port uses the strategy volume as a unit size and converts the current net position into an approximate order count.
* Only market orders are sent (`ExecutionMode.Market`). Pending order options (`OrdType=1/2` in the EA) are intentionally excluded because StockSharp documentation recommends market execution when using the high-level API.

## Porting notes and deviations

* Money management options (`MMMethod`, `Risk`, `MeansStep`) are not reproduced; StockSharp’s `Volume` property is used instead. Configure position size through the `BaseOrderVolume` parameter or the GUI.
* The MQL EA managed a pool of pending orders. The StockSharp version concentrates on net positions. You can still manually add limit/stop orders if required.
* Multi-symbol support and multi-timeframe feeds (`MW_Mode`, auxiliary MA for pending orders) are outside the scope of this port.
* Indicator buffer access does not rely on `GetValue` calls. Values are collected from `IIndicatorValue` instances using reflection so any StockSharp indicator with public `decimal` or `double` properties can serve as a buffer source.

## Parameters summary

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Primary timeframe (`TimeSpan.TimeFrame()` by default 1 hour). |
| `EntryIndicatorName`, `EntryIndicatorParameters` | Indicator type and settings for entries. |
| `EntryMode`, `EntryShift`, buffer indexes, levels | Control how entry buffers are interpreted. |
| `CloseUseOpenIndicator`, `CloseIndicatorName`, `CloseMode`, etc. | Define exit logic. |
| `CheckProfit`, `MinimalProfit`, `CheckStopDistance`, `MinimalStopDistance` | Filters for discretionary exits. |
| `SleepBars`, `CancelSleeping`, `MaxOrdersCount`, `MaxBuyCount`, `MaxSellCount` | Operational constraints. |
| `StopLossPoints`, `TakeProfitPoints`, `BaseOrderVolume` | Core risk and sizing parameters. |
| `TrailingStopEnabled`, `TrailingStartPoints`, `TrailingDistancePoints` | Price-based trailing stop. |
| `BreakEvenEnabled`, `BreakEvenStartPoints`, `BreakEvenLockPoints` | Break-even management. |
| `IndicatorTrailingEnabled`, trailing indicator options | Indicator-based trailing stop. |

## Usage tips

1. Assign the target `Security`, `Portfolio` and `Connector` as usual for StockSharp strategies.
2. Set `BaseOrderVolume` (or the base `Volume` property) to the desired lot size.
3. Define the indicator type names and parameter strings. For example, to replicate an RSI crossing a signal SMA:
   * `EntryIndicatorName = "StockSharp.Algo.Indicators.RSI"`
   * `EntryIndicatorParameters = "Length=14"`
   * `EntryMode = Cross`, `EntryMainBufferIndex = 0`
   * `CloseUseOpenIndicator = true`, `CloseMode = Cross`
4. Adjust buffers and levels to match the indicator output. For complex indicators (e.g. Bollinger Bands) check the public properties to map buffer indexes correctly.
5. Enable trailing modules if they are needed in your trading plan.

With these settings the StockSharp version behaves like the original expert advisor while fully complying with repository guidelines (high-level API, `SubscribeCandles`, indicator bindings and chart rendering).
