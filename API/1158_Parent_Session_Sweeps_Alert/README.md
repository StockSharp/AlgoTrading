# Parent Session Sweeps Alert
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors daily sessions and detects when the current session sweeps the previous session's high or low. When a sweep occurs and the candle closes back inside the prior range, a trade is opened in the opposite direction with a configurable risk-reward ratio.

## Details

- **Entry Criteria**: Sweep of previous session high/low with optional candle close filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop at session extreme or target based on risk-reward.
- **Stops**: Yes.
- **Default Values**:
  - `MinRiskReward` = 1
  - `UseCandleFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Price Action
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
