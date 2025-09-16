# Eugene Candle Pattern Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades a candlestick pattern described by "Eugene". The algorithm analyses the last four candles, checks for insider bars and special "bird" formations, and calculates breakout levels. Positions are opened on breakouts of the previous candle extremes when additional confirmation conditions are met. Optional stop loss and take profit levels are expressed in price steps.

## Details

- **Entry Criteria**:
  - Long: current high above previous high, previous low below earlier high, current low above previous low, and confirmation by zig level or time filter.
  - Short: current low below previous low, previous high above earlier low, current high below previous high, and confirmation by zig level or time filter.
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: sell when an opposite signal appears or stop loss/take profit is reached.
  - Short: buy when an opposite signal appears or stop loss/take profit is reached.
- **Stops**: fixed distance in price steps
- **Default Values**:
  - `Volume` = 1m
  - `StopLossPoints` = 0
  - `TakeProfitPoints` = 0
  - `InvertSignals` = false
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: None
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: Intraday (hour >= 8 filter)
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
