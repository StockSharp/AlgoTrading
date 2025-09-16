# Simple Levels Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Opens trades when the price crosses user defined trend lines. Each line can trigger long, short or both directions. Stop loss and take profit are set in price steps.

## Details

- **Entry Criteria**: Price crossing a configured trend line
- **Long/Short**: Determined by line direction (Buy/Sell/Both)
- **Exit Criteria**: Stop loss or take profit levels
- **Stops**: Yes
- **Default Values**:
  - `StopLoss` = 300 steps
  - `TakeProfit` = 900 steps
  - `Volume` = 1
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Levels
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Usage

1. Create and configure trend lines via `AddLine`.
2. Start the strategy to monitor incoming candles.
3. When price crosses an active line in the specified direction, the strategy sends a market order.
4. Position is closed when stop loss or take profit is reached.
