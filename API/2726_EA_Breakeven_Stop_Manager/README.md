# EA Breakeven Stop Manager Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy mirrors the MetaTrader 5 `eaBreakeven` expert advisor. It does **not** generate trade entries on its own. Instead, it continuously watches the current position and automatically locks in profit by moving the stop level to the entry price plus a configurable safety buffer once the trade reaches a defined amount of unrealized gains.

## Core Idea

- Designed for traders who want StockSharp to manage protective stops while another system handles entries.
- Works with both long and short positions on any instrument that provides best bid/ask quotes.
- Uses the instrument's price step to translate "points" into actual price increments, just like the original MQL5 implementation.

## How It Works

1. Subscribes to Level1 data and tracks the latest best bid and best ask prices.
2. When no position is open, the strategy stays idle.
3. For a **long position**:
   - Waits until the best bid is at least `BreakevenPoints` away from the average entry price.
   - Once the threshold is met, stores a breakeven stop price at `entry price + DistancePoints`.
   - If the bid falls back to that level, the position is closed at market to preserve the locked-in profit.
4. For a **short position**:
   - Waits until the best ask is `BreakevenPoints` below the average entry price.
   - Sets the protective stop at `entry price - DistancePoints`.
   - Closes the short position if the ask rallies back to that stop level.
5. Optional notifications write an informational log entry whenever a stop is moved to breakeven.

This behaviour matches the original EA, which modified the platform stop-loss order. In StockSharp the strategy simply sends a market order when the stored stop level is breached, producing the same economic result.

## Parameters

- `BreakevenPoints` (default **15**)
  - Number of points of open profit required before switching the stop to breakeven.
  - Set to a negative value to disable the breakeven logic entirely.
- `DistancePoints` (default **5**)
  - Additional buffer, measured in points from the entry price, that defines how much profit is locked in.
- `EnableNotifications` (default **true**)
  - If enabled the strategy writes a log message every time the stop is updated.

## Signals & Orders

- **Entries**: none. The strategy expects external logic to open positions.
- **Exits**: market orders submitted when the calculated breakeven stop is touched.
- **Order Types**: market orders only; no pending orders are registered.

## Risk Management

- Breakeven is only triggered once the trade accrues enough unrealized profit, preventing premature exits.
- The buffer keeps the protective stop slightly inside profitable territory to avoid being closed exactly at the entry price.
- Once the position is flat all internal state is reset, so the next trade starts fresh.

## Usage Tips

- Combine with any entry strategy that leaves stop-loss management to external code.
- Works in both live trading and backtests as long as Level1 (bid/ask) data is available.
- Because the stop logic relies on price steps, verify that the instrument's `PriceStep` property is set correctly in the security metadata.

## Default Filters

- Category: Risk management
- Direction: Long & Short
- Indicators: None
- Stops: Breakeven
- Complexity: Beginner
- Timeframe: Agnostic (works on ticks, intraday, or daily data)
- Seasonality: No
- Neural networks: No
- Divergence: No
- Risk level: Low
