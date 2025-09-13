# Straddle News Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy designed for high-volatility news releases. It places symmetric stop orders on both sides of the current price to catch breakouts. Once one order triggers, the opposite pending order is cancelled and a trailing stop protects the open position.

## Details

- **Entry Criteria**: wait for spread below `SpreadOperation`, then place buy stop at Ask + `PipsAway` points and sell stop at Bid - `PipsAway` points
- **Long/Short**: Both
- **Exit Criteria**: protective stop loss or take profit, or trailing stop when price retraces by `TrailingStop` points
- **Stops**: Initial stop loss and take profit via `StartProtection`; custom trailing stop in code
- **Default Values**:
  - `StopLoss` = 100
  - `TakeProfit` = 300
  - `TrailingStop` = 50
  - `PipsAway` = 50
  - `BalanceUsed` = 0.01
  - `SpreadOperation` = 25
  - `Leverage` = 400
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Level1 / Tick
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High

## How It Works

1. Subscribe to Level1 quotes to access current bid and ask prices.
2. When spread is small enough, calculate volume using portfolio value, leverage and `BalanceUsed`.
3. Place pending buy and sell stop orders at offsets defined by `PipsAway`.
4. When a position opens, cancel the opposite pending order.
5. Attach stop loss and take profit orders based on `StopLoss` and `TakeProfit`.
6. Track highest/lowest price since entry and exit if price retraces more than `TrailingStop` points.
