# FT TIME BIGDOG Breakout Strategy

## Overview
The **FT TIME BIGDOG** strategy is a London-session breakout system converted from the MetaTrader 4 expert advisor `FT_TIME_BIGDOG.mq4` (directory `MQL/9259`).
It measures the consolidation range that forms between the configured start and stop hours and then places stop orders above and below that range once the window closes.
The StockSharp version keeps the original behaviour while exposing configurable parameters for breakout timing, order distance and risk management.

## Trading Logic
1. On every trading day the strategy records the highest high and lowest low of finished candles whose opening hour lies between **StartHour** and **StopHour** (inclusive).
2. After the stop hour candle finishes, if the accumulated range is narrower than **RangeLimitPoints**, two pending stop orders become eligible:
   - A **buy stop** at the recorded high.
   - A **sell stop** at the recorded low.
3. Orders are created only if the market price is at least **OrderBufferPoints** away from the entry level. Best bid/ask prices are used when available, otherwise the latest candle close is used.
4. Each pending order includes a protective stop at the opposite side of the range and a take profit offset defined by **TakeProfitPoints**.
5. When a position is opened, the opposite pending order is cancelled. The active position is monitored on finished candles: if price touches the stored stop loss or take profit level the position is closed at market.
6. The cycle runs at most once per day; all state is reset at the start of the next trading day.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `StartHour` | 14 | Hour (0–23) marking the beginning of the accumulation window. |
| `StopHour` | 16 | Hour when pending orders become eligible. Must be greater than or equal to `StartHour`. |
| `RangeLimitPoints` | 50 | Maximum width of the session range in broker points (points × `PointMultiplier`). No orders are placed if the range is wider. |
| `TakeProfitPoints` | 50 | Take-profit distance applied to triggered positions, expressed in broker points. |
| `OrderBufferPoints` | 20 | Minimum distance required between the market price and a pending order. Prevents orders from being placed too close to current price. |
| `PointMultiplier` | 1 | Multiplier applied to the instrument point size. Set to 10 for five-digit forex symbols. |
| `Volume` | 0.1 | Order volume for both stop orders. |
| `CandleType` | 1 hour | Candle series used to measure the range and drive signal evaluation. |

## Risk and Trade Management
- Stop loss for long trades equals the session low; stop loss for short trades equals the session high.
- Take profit levels are calculated from the breakout price using `TakeProfitPoints` and the instrument point size.
- All risk controls are executed on candle close events; intrabar excursions beyond stop levels may result in delayed exits.

## Differences vs. Original Expert Advisor
- The MetaTrader version operates on tick events while this port relies on finished candles and level 1 updates. Behaviour inside a candle may therefore differ slightly.
- Point conversion uses `Security.PriceStep` multiplied by `PointMultiplier`. Verify this combination before running live.
- Orders are placed only when `StartHour <= StopHour`. Cross-midnight windows are not supported in this port.

## Usage Notes
1. Assign the desired security and verify that level 1 data is available for accurate buffer checks.
2. Configure trading hours according to the broker time zone.
3. Run in simulation first to validate the point conversion and timing relative to your data feed.
4. Reset or stop the strategy before manually altering pending orders to avoid conflicting state.

## Files
- `CS/FtTimeBigdogStrategy.cs` – core StockSharp implementation with detailed inline comments.
- `MQL/9259/FT_TIME_BIGDOG.mq4` – original MetaTrader source used for the conversion.
