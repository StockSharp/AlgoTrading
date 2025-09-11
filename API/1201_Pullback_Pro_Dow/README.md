# Pullback Pro Dow Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses Dow Theory pivots to define trend direction and enters on EMA pullbacks when trend strength is confirmed by ADX. The system scales out at two risk-reward targets.

Backtests show steady behavior on index futures like US30.

## Details

- **Entry Criteria**:
  - Long: higher highs and higher lows, low crosses below EMA, ADX above threshold
  - Short: lower highs and lower lows, high crosses above EMA, ADX above threshold
- **Long/Short**: Both
- **Exit Criteria**: Stop at last pivot, take profit at two R:R targets
- **Stops**: Pivot-based
- **Default Values**:
  - `PivotLookback` = 10
  - `EmaLength` = 21
  - `RiskReward1` = 1.5m
  - `Tp1Percent` = 50
  - `RiskReward2` = 3m
  - `UseAdxFilter` = true
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, Average Directional Index
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
