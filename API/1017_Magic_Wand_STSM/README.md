# Magic Wand STSM Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A trend-following system using the Supertrend indicator with a 200-period SMA filter. It trades in the direction of the Supertrend and uses the line as a stop, targeting a configurable risk–reward take profit.

## Details

- **Entry Criteria**:
  - **Long**: Supertrend below price and close above SMA200.
  - **Short**: Supertrend above price and close below SMA200.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Take profit at `entry ± (entry - Supertrend) * RiskReward`.
  - Stop loss at Supertrend.
- **Stops**: Yes.
- **Default Values**:
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
