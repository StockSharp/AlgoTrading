# Bollinger Bands Distance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trading Bollinger Bands reversals with an extra distance filter. Sells when price closes above the upper band plus a set distance and buys when it closes below the lower band minus the same distance. Positions are closed by profit target or stop loss measured in price steps.

## Details

- **Entry Criteria**:
  - Long: close below lower Bollinger Band minus distance
  - Short: close above upper Bollinger Band plus distance
- **Long/Short**: Both
- **Exit Criteria**:
  - Profit target reached
  - Stop loss reached
- **Stops**: Absolute in price steps
- **Default Values**:
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Reversion
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
