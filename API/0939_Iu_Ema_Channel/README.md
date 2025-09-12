# IU EMA Channel
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from TradingView script "IU EMA Channel Strategy". The strategy trades when price crosses EMA channels built from highs and lows. Stop-loss is set at the previous candle extreme and take-profit is calculated using a risk-to-reward ratio.

## Details

- **Entry Criteria**: Close crosses above high EMA for long, below low EMA for short.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss at previous candle extreme or take-profit by risk-to-reward ratio.
- **Stops**: Yes, fixed stop and target.
- **Default Values**:
  - `EmaLength` = 100
  - `RiskToReward` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Variable
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
