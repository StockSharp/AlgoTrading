# Levels With Revolve Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens trades when the market price crosses a user defined level. A buy order is placed when price rises through the level and a sell order when price falls through it. The system can optionally reverse an existing position if the opposite signal appears. It also supports optional stop loss and take profit distances measured in price units.

The strategy subscribes to candles and reacts only when a candle is fully formed. All calculations are performed on the close price of each finished candle. When reversal mode is enabled the current position is closed and a new position in the opposite direction is opened on the next signal.

## Details

- **Entry Criteria**:
  - Long: close price crosses above `LevelPrice`.
  - Short: close price crosses below `LevelPrice`.
- **Long/Short**: Both directions.
- **Reversal**: Optional, controlled by `EnableReversal`.
- **Stops**: Optional stop loss and take profit in price units.
- **Default Values**:
  - `LevelPrice` = 100.
  - `StopLoss` = 0 (disabled).
  - `TakeProfit` = 0 (disabled).
  - `EnableReversal` = false.
  - `CandleType` = 1 minute time frame.
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Optional
  - Complexity: Simple
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
