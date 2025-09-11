# Daily Supertrend Ema Crossover Rsi Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading EMA crossovers only when Supertrend confirms the direction and RSI is favorable. Uses ATR-based stop loss and take profit levels.

## Details

- **Entry Criteria**:
  - Long: `Fast EMA` crosses above `Slow EMA`, Supertrend uptrend, `RSI < RsiOverbought`
  - Short: `Fast EMA` crosses below `Slow EMA`, Supertrend downtrend, `RSI > RsiOversold`
- **Long/Short**: Both
- **Exit Criteria**: ATR-based stop loss or take profit
- **Stops**: Yes
- **Default Values**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, Supertrend, RSI, ATR
  - Stops: ATR multiples
  - Complexity: Intermediate
  - Timeframe: Long-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
