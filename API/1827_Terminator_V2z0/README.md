# Terminator V2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Moving Average Convergence Divergence (MACD) oscillator to trade in both directions. A long position is opened when the MACD line crosses above its signal line. A short position is opened when the MACD line crosses below its signal line. Positions are protected by fixed stop-loss and take-profit levels, while an optional trailing stop can lock in profits during strong trends.

## Details

- **Entry Criteria**:
  - **Long**: `MACD` crosses above the signal line.
  - **Short**: `MACD` crosses below the signal line.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop-loss or take-profit level is reached.
  - Trailing stop is triggered.
- **Stops**: Yes, includes stop-loss, take-profit, and optional trailing stop.
- **Default Values**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 26
  - `SignalPeriod` = 1
  - `TakeProfit` = 500 points
  - `StopLoss` = 2500 points
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Moderate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
