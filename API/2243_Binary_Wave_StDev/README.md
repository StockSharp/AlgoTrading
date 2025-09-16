# Binary Wave StdDev Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that sums signals from MA, MACD, CCI, Momentum, RSI and ADX using configurable weights.
Trades in direction of cumulative score when volatility measured by standard deviation exceeds a threshold.
Optional stop loss and take profit in points.

## Details

- **Entry Criteria**:
  - Long: score > 0 and StdDev >= EntryVolatility
  - Short: score < 0 and StdDev >= EntryVolatility
- **Exit Criteria**:
  - Volatility falls below ExitVolatility
- **Stops**: Optional via `UseStopLoss` and `UseTakeProfit`
- **Default Values**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - Stops: Optional
  - Complexity: Medium
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
