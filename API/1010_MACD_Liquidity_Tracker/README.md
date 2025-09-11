# MACD Liquidity Tracker
[Русский](README_ru.md) | [中文](README_cn.md)

MACD Liquidity Tracker uses MACD colour states to generate trading signals. Four modes (Fast, Normal, Safe, Crossover) adjust signal sensitivity. Optional stop loss and take profit are supported.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Depends on `SystemType` (default `Normal` uses MACD above signal).
  - **Short**: Depends on `SystemType` (default `Normal` uses MACD below signal).
- **Exit Criteria**: Opposite signal.
- **Stops**: Optional stop loss and take profit.
- **Default Values**:
  - `FastLength` = 25
  - `SlowLength` = 60
  - `SignalLength` = 220
  - `AllowShortTrades` = false
  - `SystemType` = Normal
  - `UseStopLoss` = false
  - `StopLossPercent` = 3
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 6
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `CandleType` = tf(5)
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
