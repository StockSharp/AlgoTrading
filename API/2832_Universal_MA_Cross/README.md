# Universal MA Cross Strategy

## Overview
The **Universal MA Cross Strategy** is a direct conversion of the original MQL5 expert advisor "UniversalMACrossEA" into the StockSharp high-level strategy framework. The algorithm compares a fast and a slow moving average that can be configured with different calculation methods and price sources. Optional filters control how signals are confirmed, whether trades are reversed immediately, how risk management is performed and when the strategy is allowed to trade.

## Trading Logic
### Indicator processing
* Two moving averages are calculated on the selected candle series. Each average can use its own period, smoothing method (SMA, EMA, SMMA or LWMA) and price type (close, open, high, low, median, typical or weighted).
* The parameter **MinCrossDistance** requires the fast and slow averages to diverge by at least the specified number of price units at the crossover bar.
* When **ConfirmedOnEntry** is enabled the crossover is validated on the previous completed bar (equivalent to using bar indexes 2 and 1 in the original EA). If it is disabled, the current finished bar is compared with the previous bar, replicating the "tick mode" behaviour of the MQL version.
* Setting **ReverseCondition** swaps the bullish and bearish signals so that the rules can be inverted without changing any indicator settings.

### Entry rules
1. For a long entry the fast average must cross above the slow average by at least **MinCrossDistance**. For a short entry the fast average must cross below the slow average by that distance.
2. When **StopAndReverse** is enabled and an opposite signal arrives, the active position is closed before new orders are considered.
3. If **OneEntryPerBar** is true, the strategy remembers the bar time of the latest entry and refuses to open another trade during the same candle.
4. The volume of each order is configured by the **Volume** parameter.

### Position management
* Stop-loss and take-profit levels are measured in price units. They are ignored when **PureSar** is true, matching the "Pure SAR" mode of the original expert.
* Trailing stop logic activates after the price moves by **TrailingStop + TrailingStep** from the entry price. Every additional move of at least **TrailingStep** points tightens the stop by the specified **TrailingStop** distance. Trailing does not run in "Pure SAR" mode.
* Protective levels are monitored on every finished candle. If the candle range violates the stop-loss or take-profit level the position is closed by market order.

### Session filter
* When **UseHourTrade** is enabled the strategy trades only when the candle opening hour is between **StartHour** and **EndHour** (inclusive). The trailing stop management continues to run outside of that interval, but no new entries or stop-and-reverse actions are executed.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Periods of the fast and slow moving averages. |
| `FastMaType`, `SlowMaType` | Moving average methods: Simple, Exponential, Smoothed (RMA) or Linear Weighted. |
| `FastPriceType`, `SlowPriceType` | Price sources fed into the averages. |
| `StopLoss`, `TakeProfit` | Protective distances in absolute price units. Set to 0 to disable. |
| `TrailingStop`, `TrailingStep` | Trailing stop offset and minimum extra move required before shifting the stop. |
| `MinCrossDistance` | Minimum distance between the averages at the crossover bar. |
| `ReverseCondition` | Swap bullish and bearish rules. |
| `ConfirmedOnEntry` | Use only completed bars for validation. |
| `OneEntryPerBar` | Allow at most one entry per candle. |
| `StopAndReverse` | Close the current position and reverse on opposite signals. |
| `PureSar` | Disable stop-loss, take-profit and trailing logic. |
| `UseHourTrade`, `StartHour`, `EndHour` | Time filter for trading sessions (0–23 hours). |
| `Volume` | Order volume for each position. |
| `CandleType` | Candle data type subscribed for calculations. |

## Conversion Notes
* Protective orders are handled internally by checking candle highs and lows, because StockSharp strategies operate on finalized candles instead of raw tick events. This mirrors the behaviour of the original expert while staying within the high-level API.
* Trailing stop adjustments follow the MQL implementation, requiring a move of **TrailingStop + TrailingStep** before the stop is shifted.
* No Python version is provided in this conversion as requested.
