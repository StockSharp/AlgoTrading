# Genie Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Genie is a Parabolic SAR expert advisor enhanced with the Average Directional Index (ADX) to confirm trend strength. The strategy opens positions when the SAR flips relative to price while the +DI and -DI components of ADX change dominance. A trailing stop and fixed take profit manage risk.

Testing shows that the approach works best on trending instruments with moderate volatility.

## Details

- **Entry Criteria**:
  - **Long**: Previous SAR above prior close, current SAR below current close, previous +DI < previous -DI, current +DI > current -DI, and ADX above both current +DI and -DI.
  - **Short**: Previous SAR below prior close, current SAR above current close, previous +DI > previous -DI, current +DI < current -DI, and ADX above both current +DI and -DI.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Trailing stop hits or previous candle closes against the position.
- **Stops**: Yes, trailing stop and take profit measured in price units.
- **Default Values**:
  - `TakeProfit` = 500
  - `TrailingStop` = 200
  - `SarStep` = 0.02
  - `AdxPeriod` = 14
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic SAR, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes (between +DI and -DI)
  - Risk level: Medium
