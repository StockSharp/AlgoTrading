# Profitable SuperTrend + MA + Stoch Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining SuperTrend, moving average crossover and Stochastic oscillator.

It aims to capture trends identified by SuperTrend and confirm entries with EMA crossover and Stochastic levels. Includes optional take profit and stop loss targets.

## Details

- **Entry Criteria**: Trend by SuperTrend, EMA crossover, stochastic thresholds.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite EMA crossover or TP/SL.
- **Stops**: Yes.
- **Default Values**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `MaFastPeriod` = 9
  - `MaSlowPeriod` = 21
  - `StochKPeriod` = 14
  - `StochDPeriod` = 3
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SuperTrend, EMA, Stochastic
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
