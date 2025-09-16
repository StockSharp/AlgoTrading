# MACD vs Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on MACD line crossing the signal line.

Enters long when the MACD line crosses above the signal line.
Enters short when the MACD line crosses below the signal line.
Optionally applies stop loss, take profit and trailing stop.

## Details

- **Entry Criteria**:
  - Long: `MACD crosses above Signal`
  - Short: `MACD crosses below Signal`
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite MACD crossover
  - Risk management rules (stop loss, trailing stop, take profit)
- **Stops**: Stop loss, take profit, trailing stop (optional)
- **Default Values**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 50 points
  - `TakeProfit` = 999 points
  - `TrailingStop` = 0 points (disabled)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD
  - Stops: Stop loss / Take profit / Trailing
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
