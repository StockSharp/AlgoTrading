# Universal MA Cross V4 Strategy

## Overview
The **Universal MA Cross V4 Strategy** is a high-level StockSharp port of the MetaTrader 4 expert advisor "Universal MACross EA v4". The algorithm follows the interaction between a configurable fast moving average and a slow moving average. It supports several moving average types, selectable price sources, an hourly trading window, and flexible position management including stop-and-reverse behaviour, protective targets and trailing stops. The strategy is designed for bar-based execution using the StockSharp high-level API with candle subscriptions.

## Trading Logic
### Indicator processing
* Two moving averages are evaluated on every finished candle. Each moving average can use its own length, smoothing method (Simple, Exponential, Smoothed or Linear Weighted) and price source (close, open, high, low, median, typical or weighted).
* The **MinCrossDistancePoints** filter requires the fast and slow averages to diverge by at least the specified number of price steps at the crossover bar. When **ConfirmedOnEntry** is enabled the divergence is validated on the previous completed candle, reproducing the "confirmed" mode from the original EA.
* Setting **ReverseCondition** swaps bullish and bearish signals without changing the indicator configuration.

### Entry rules
1. A long entry occurs when the fast average crosses above the slow average by at least **MinCrossDistancePoints**. A short entry requires the opposite cross.
2. When **StopAndReverse** is true, an opposite signal closes the active position before new entries are considered.
3. **OneEntryPerBar** prevents multiple entries inside the same candle by tracking the timestamp of the most recent order.
4. The order size is controlled by **TradeVolume**. StockSharp automatically applies this volume to the generated market orders.

### Position management
* Stop-loss and take-profit distances are defined in points through **StopLossPoints** and **TakeProfitPoints**. They are converted into absolute prices using the instrument price step. When **PureSar** is active all protective logic is disabled, just like the "Pure SAR" option in the MQL version.
* Trailing stop management mirrors the MQL implementation: once price moves further than **TrailingStopPoints** from the entry level the stop is pulled behind the market by the same distance. Trailing stops are ignored when **PureSar** is enabled.
* Protective levels are monitored on every closed candle. If the candle range violates the active stop or target the strategy closes the position by market order to maintain deterministic behaviour on historical data.

### Session filter
* The **UseHourTrade** flag restricts trading to the inclusive window between **StartHour** and **EndHour** (0–23). Session bounds wrap around midnight when the end hour is smaller than the start hour. Position management, including trailing stops, remains active outside the session, but no new entries are allowed.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Lengths of the fast and slow moving averages. |
| `FastMaType`, `SlowMaType` | Moving average methods: Simple, Exponential, Smoothed or Linear Weighted. |
| `FastPriceType`, `SlowPriceType` | Price sources fed into each moving average. |
| `StopLossPoints`, `TakeProfitPoints` | Protective distances in price steps. Set to `0` to disable. |
| `TrailingStopPoints` | Trailing stop distance in price steps. Set to `0` to disable trailing. |
| `MinCrossDistancePoints` | Minimum separation between the averages required to validate a cross. |
| `ReverseCondition` | Swap bullish and bearish rules without changing indicators. |
| `ConfirmedOnEntry` | Validate signals on the previous closed bar. Disable for immediate confirmation. |
| `OneEntryPerBar` | Allow at most one new position per candle. |
| `StopAndReverse` | Close and reverse the current position when the opposite signal appears. |
| `PureSar` | Disable stop-loss, take-profit and trailing stop logic. |
| `UseHourTrade`, `StartHour`, `EndHour` | Session filter that restricts entries to a specific hour range. |
| `TradeVolume` | Order volume used by `BuyMarket` and `SellMarket`. |
| `CandleType` | Candle series subscribed for indicator calculations. |

## Conversion Notes
* Price-based distances are expressed in MetaTrader points. The helper `GetPriceOffset` converts those values into StockSharp prices using the security price step or decimal precision. This keeps the strategy behaviour aligned with the original EA regardless of instrument.
* Trailing stops are managed internally because StockSharp high-level strategies operate on finished candles. This deterministic approach ensures that backtests using candles reproduce the intended MT4 trailing logic.
* No Python port is included, matching the conversion request. Only the C# implementation and multilingual documentation are provided in this package.
