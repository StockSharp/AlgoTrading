# Supertrend And MACD
[English](README.md) | [Русский](README_ru.md)

结合 Supertrend、MACD 和 EMA 200 过滤器的策略。

## 详情

- **入场条件**: 价格相对 Supertrend 和 EMA，MACD 线与信号线比较。
- **多空方向**: 双向。
- **退出条件**: MACD 交叉或基于最近极值的止损。
- **止损**: Highest/Lowest 跟踪止损。
- **默认值**:
  - `AtrPeriod` = 10
  - `Factor` = 3
  - `EmaPeriod` = 200
  - `StopLookback` = 10
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - Category: Trend
  - Direction: Both
  - Indicators: SuperTrend, EMA, MACD, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
