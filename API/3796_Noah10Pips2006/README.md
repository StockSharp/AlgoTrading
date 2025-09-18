# Noah 10 Pips 2006 Strategy

## Summary
- Recreates the breakout and reversal logic from the original Noah10pips2006 MetaTrader 4 expert advisor.
- Builds previous-session price channels and places stop orders around the mid-point.
- Applies secure-profit trailing, optional dynamic position sizing, and an optional reversal trade after the first position closes.

## Trading Logic
1. **Session Range Calculation**  
   At the start of every new trading day (after applying the configured time-zone offset) the strategy records the previous session high and low. These levels are used to compute:
   - The mid-point between high and low.  
   - A "pass" buffer positioned 20 pips above/below the range.  
   - An entry channel obtained by subtracting/adding 40 pips (or 25% of the range if the range is larger than 160 pips).
2. **Initial Pending Order**  
   When the market enters the trading window, the strategy checks the latest close:  
   - If the close is between the mid-point and the upper buffer, a **sell stop** is placed at the mid-point.  
   - If the close is between the lower buffer and the mid-point, a **buy stop** is placed at the mid-point.  
   The range width must exceed the configured minimum before any orders are placed.
3. **Second Pending Order**  
   If only one stop order remains active, the system adds the opposite-direction order at the corresponding buffer (upper buffer for a buy stop, lower buffer for a sell stop). This mirrors the original EA behaviour and prepares the strategy for breakouts on both sides of the range.
4. **Position Management**  
   - Protective stop-loss and take-profit orders are created after an entry fills.  
   - Once floating profit reaches the secure trigger threshold, the stop-loss is moved to lock in the configured secure profit.  
   - When the secure lock is active, an optional trailing stop keeps following price with the specified distance.
5. **Daily Shutdown**  
   All pending orders and open positions are closed when the trading window ends or when the Friday cut-off is reached.
6. **Reversal Trade**  
   The first completed position can trigger an opposite-direction market order, reproducing the "reverse after stop" behaviour from the original code. The reversal is skipped if the secure-profit adjustment already locked in gains.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used to drive calculations and timing. Default: 1-hour candles. |
| `TimeZoneOffset` | Shift (in hours) applied to exchange timestamps before daily calculations. |
| `StartHour`, `StartMinute` | Opening time of the trading window in the shifted time-zone. |
| `EndHour`, `EndMinute` | Closing time of the trading window. New entries are not placed afterwards. |
| `FridayEndHour` | Hour on Friday when positions are force-closed. |
| `TradeFriday` | Enables or disables opening new positions on Friday. |
| `StopLossPips`, `TakeProfitPips` | Distance (in pips) of protective orders created after entry. |
| `TrailingStopPips` | Trailing-stop distance used after the secure-profit step. Set to 0 to disable trailing. |
| `SecureProfitPips` | Profit locked when the secure trigger activates. |
| `TrailSecureProfitPips` | Profit threshold required before moving the stop to the secure level. |
| `MinimumRangePips` | Minimum width of the entry channel required to place orders. |
| `StartYear`, `StartMonth` | Ignore market data that is older than this date. |
| `MinVolume`, `MaxVolume` | Bounds applied to the computed order volume. |
| `MaximumRiskPercent` | Percentage of portfolio value risked per trade when dynamic sizing is enabled. |
| `FixedVolume` | When `true`, the strategy uses the `Volume` property instead of the risk model. |

## Practical Notes
- The instrument must provide valid `PriceStep` and `StepPrice` values when the risk-based position sizing mode is used.
- Trailing and secure-profit adjustments rely on completed candles, so intrabar fills are processed on the next finished candle.
- The strategy cancels and replaces protective orders whenever the trailing logic updates the stop price.
- Ensure the time-zone offset matches the source of historical data; otherwise, the previous-day range can differ from the original MT4 expert.

## Conversion Caveats
- Visual drawing objects from the MT4 version were omitted; use the supplied levels or add custom chart annotations if needed.
- The algorithm assumes four-digit Forex quoting when converting the fixed 20/40 pip buffers; adjust parameters for different asset classes.
- Reverse trades execute at market with the current volume model, matching the behaviour of the original EA after deleting opposite pending orders.
