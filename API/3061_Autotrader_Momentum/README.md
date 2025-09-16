# Autotrader Momentum Strategy

## Overview
The **Autotrader Momentum Strategy** is a conversion of the MetaTrader 5 expert advisor *Autotrader Momentum (barabashkakvn's edition)*. The algorithm evaluates recent momentum by comparing the closing price of the monitoring bar with the closing price of a historical reference bar. When a bullish momentum shift is detected, the strategy buys; when a bearish shift appears, it sells. All orders are executed at market price using StockSharp's high-level trading API.

The implementation keeps the original focus on point-based risk control. Stop-loss, take-profit, and trailing-stop distances are defined in pips and automatically translated into price offsets based on the instrument's `PriceStep`. Support for three and five decimal quotes is preserved by applying the same 10x adjustment used in the MQL code. Trailing logic is evaluated on every finished candle before new entries are considered, ensuring that risk management mirrors the EA's behaviour of prioritizing protective exits.

## Trading Logic
1. Subscribe to the configured `CandleType` and process only finished candles, matching the "new bar" logic of the original EA.
2. Maintain a rolling window of closing prices sized to `max(CurrentBarIndex, ComparableBarIndex) + 1`.
3. Compare the close of the monitored bar (`CurrentBarIndex`, default 0) with the close of the historical bar (`ComparableBarIndex`, default 15).
4. If the monitored close is greater than the reference close, close any short exposure and buy the configured trade volume.
5. If the monitored close is less than the reference close, close any long exposure and sell the configured trade volume.
6. Each entry recalculates average entry price and refreshes stop-loss, take-profit, and trailing-stop levels.

Because StockSharp strategies work with a net position, reversals combine the volume required to close the opposite exposure with the configured base volume. This matches the MQL behaviour that first closed the opposite side and then opened a fresh order of the requested size.

## Parameters
- `CandleType` – Time frame used for price comparison. Default: 1 hour.
- `TradeVolume` – Base market order volume. Applied on every signal in addition to any volume needed to reverse an existing position.
- `StopLossPips` – Protective stop distance in pips. Set to 0 to disable the fixed stop-loss.
- `TakeProfitPips` – Profit target distance in pips. Set to 0 to disable the fixed take-profit.
- `TrailingStopPips` – Distance maintained by the trailing stop. Set to 0 to disable trailing.
- `TrailingStepPips` – Minimum favourable move required before the trailing stop is advanced. Must be positive when trailing is enabled.
- `CurrentBarIndex` – Index of the monitoring candle (0 = most recent finished bar).
- `ComparableBarIndex` – Index of the historical bar used for momentum comparison.

All pip-based settings are converted into price offsets using the instrument's `PriceStep`. If the step represents three or five decimal digits, the offset is multiplied by 10 to reproduce the MetaTrader definition of a pip.

## Risk Management
- **Fixed Stops and Targets:** Whenever `StopLossPips` or `TakeProfitPips` are greater than zero, the strategy maintains corresponding price levels relative to the averaged entry price.
- **Trailing Stop:** Enabled when both `TrailingStopPips` and `TrailingStepPips` are positive. The trailing logic moves the protective stop only after the price has moved by at least `TrailingStopPips + TrailingStepPips` from the averaged entry price, replicating the EA requirement that ensured the move is large enough before tightening the stop.
- **State Reset:** Any time the position returns to zero—either via strategy-driven exits or external intervention—the cached risk state is cleared to avoid stale stop or take-profit levels.

## Implementation Notes
- The strategy relies exclusively on StockSharp's high-level market API (`BuyMarket`, `SellMarket`) and avoids indicator collections to remain faithful to the conversion guidelines.
- Close prices are buffered in a simple rolling list so that `CurrentBarIndex` and `ComparableBarIndex` can be changed at runtime without requiring a restart.
- Because StockSharp operates on a net position, stop-loss and take-profit levels are tracked for the aggregate exposure. When additional orders are layered in the same direction, the code recomputes a volume-weighted average entry price before refreshing the risk levels.
- Trailing-stop adjustments and protective exits are processed before new signals on each candle, preventing new entries from being evaluated when an exit has already been issued for that bar.

## Original Strategy Reference
- **Source:** `MQL/22409/Autotrader Momentum.mq5`
- **Author:** barabashkakvn (MetaTrader community)
