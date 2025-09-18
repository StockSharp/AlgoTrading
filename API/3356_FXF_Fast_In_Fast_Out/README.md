# FXF Fast in Fast out Strategy

## Overview
The **FXF Fast in Fast out** strategy is a volatility-driven breakout system that converts the original MetaTrader 4 expert advisor into a StockSharp high-level strategy. It watches a configurable timeframe for large candles, measures the spread, and reacts by placing pending stop orders that attempt to catch immediate momentum continuation. The logic uses only finished candles for signal generation while quotes (Level1 data) are used for spread filters, order placement, and trailing stop management.

When the current candle expands beyond a volatility threshold, the strategy evaluates the mid-price relative to the candle open. If the mid-price closes above the open, a buy stop is placed above the best ask; if it closes below, a sell stop is placed under the best bid. Protective stop-loss and take-profit levels are attached to pending orders, and optional trailing logic protects open positions once they are filled. Money management can dynamically size orders based on the portfolio value and stop distance.

## Trading Logic
- **Signal detection** – On every finished candle the strategy checks whether the candle range expressed in price steps exceeds `VolatilitySizePoints`. If the range is large enough, it computes the mid-price using the latest best bid/ask snapshot.
- **Directional bias** – A mid-price above the candle open produces a bullish bias (buy stop order), while a mid-price below the open produces a bearish bias (sell stop order). No order is placed if the mid-price is equal to the open or the volatility requirement is not met.
- **Spread filter** – Quotes are continuously monitored. Pending orders are created only when the current spread is below `MaxSpreadPoints`. If the spread widens beyond that limit, any existing pending orders are cancelled until the spread returns to acceptable levels.
- **Pending order management** – Only one pending order can be active per bar. Each order is offset from the best quote by `EnterOffsetPoints`. Stop-loss and take-profit distances are defined in points and automatically converted into prices.
- **Risk control** – With `UseMoneyManagement` enabled, order volume is sized from the portfolio value, risk percentage, and stop-loss distance using the instrument step price. Otherwise the default `Volume` property is used.
- **Trailing stop** – When `EnableTrailing` is true, the strategy maintains an internal trailing stop for the active position based on `TrailingStopPoints` plus the current spread. If the market price crosses the trailing stop, the position is closed at market.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `EnterOffsetPoints` | Distance in price steps between the best quote and the pending stop order price. |
| `MaxSpreadPoints` | Maximum allowed spread (in price steps). Spread above this limit blocks new entries and cancels active pending orders. |
| `TakeProfitPoints` | Take-profit distance in price steps applied to pending orders. Set to zero to skip take-profit placement. |
| `StopLossPoints` | Stop-loss distance in price steps. Required for money management sizing. Set to zero to disable stop-loss placement. |
| `VolatilitySizePoints` | Minimum candle range (in price steps) required to generate a new breakout signal. |
| `EnableTrailing` | Enables or disables the trailing stop logic for open positions. |
| `TrailingStopPoints` | Base trailing distance in price steps. The actual trailing level also includes the current spread to mimic the original EA behaviour. |
| `UseMoneyManagement` | Enables portfolio-based position sizing using the `RiskPercent` value. |
| `RiskPercent` | Risk percentage per trade used when money management is active. |
| `MaxOrdersPerBar` | Maximum number of pending orders allowed during a single bar. Typically set to 1 to mirror the original expert advisor. |
| `CandleType` | The timeframe of candles used for signal calculations. Default is 15 minutes. |

## Order Workflow
1. **Detection** – A finished candle that meets the volatility criterion sets the desired trade direction.
2. **Validation** – Quotes must be available, trading must be allowed, no open position should exist, and no other active order should be present.
3. **Placement** – The strategy places a buy stop or sell stop with the computed offset, attaching stop-loss and take-profit levels.
4. **Trailing and Exit** – After an order fills, the trailing module watches the latest quotes. Breaching the trailing level closes the position with a market order. Take-profit and stop-loss orders remain attached to the position for automatic execution by the broker or simulator.

## Notes
- The strategy requires both candle and Level1 data subscriptions to operate correctly.
- Risk-based sizing falls back to the configured `Volume` if stop-loss parameters or security metadata (price step or step price) are unavailable.
- Trailing stops are managed internally via market exits to match the MetaTrader behaviour, ensuring compatibility across different execution venues.
