# EMA 5-8-13 with ADX Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades EMA crossovers on 5 and 8 periods while using a 13-period EMA for exits. An optional ADX filter confirms trend strength. Long positions occur when EMA5 crosses above EMA8 and ADX exceeds the threshold. Short positions are initiated on the opposite signal.

## Details

- **Entry Criteria**:
  - **Long**: EMA5 crosses above EMA8 and ADX > threshold.
  - **Short**: EMA5 crosses below EMA8 and ADX > threshold.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: close < EMA13
  - **Short**: close > EMA13
- **Stops**: No.
- **Default Values**:
  - `ADX threshold` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Simple
  - Timeframe: Short-term
