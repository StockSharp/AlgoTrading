# Fib Hurst Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Fib Hurst Breakout combines Fibonacci retracement levels from the daily timeframe with a Hurst exponent filter. Price crossing the key Fibonacci levels in the direction of the prevailing trend triggers entries while a 2% stop and 1:2 risk-reward manage risk.

## Details

- **Entry Criteria**:
  - Long: Close crosses above the 61.8% level and daily Hurst > 0.5
  - Short: Close crosses below the 38.2% level and daily Hurst < 0.5
- **Long/Short**: Both
- **Exit Criteria**: Stop-loss or take-profit
- **Stops**: Yes
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Hurst, Fibonacci
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
