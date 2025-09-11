# KumoTrade Ichimoku Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Ichimoku Cloud and Stochastic Oscillator.
It enters long when price pulls back above Kijun with oversold Stochastic and no cloud ahead.
It enters short when price drops below the cloud with overbought Stochastic and bearish Kumo.

## Details

- **Entry Criteria**:
  - Long: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - Short: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **Long/Short**: Both
- **Exit Criteria**:
  - ATR based trailing stop
- **Stops**: Trailing stop using ATR * 3
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Ichimoku Cloud, Stochastic, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
