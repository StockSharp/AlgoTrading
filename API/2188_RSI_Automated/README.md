# RSI Automated Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum strategy using the Relative Strength Index (RSI) to trade extreme oversold and overbought conditions.
The system opens a long position when RSI drops below the oversold level and a short position when RSI rises above the overbought level.
Positions are closed when RSI returns to a middle threshold or when stop-loss, take profit, or trailing stop levels are triggered.

## Details

- **Entry Criteria**: RSI crossing below `Oversold` for longs or above `Overbought` for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: RSI crossing `ExitLevel`, stop-loss, take profit, or trailing stop.
- **Stops**: Yes, fixed stop-loss, take profit, and optional trailing stop.
- **Default Values**:
  - `RsiPeriod` = 14
  - `Overbought` = 75
  - `Oversold` = 25
  - `ExitLevel` = 50
  - `StopLossPoints` = 50
  - `TakeProfitPoints` = 150
  - `TrailingStopPoints` = 25
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
