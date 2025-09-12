# Indicator Test with Conditions Table Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy compares the latest closing price with user-defined levels and executes market orders when the conditions are met. Each side (long and short) has separate entry and exit rules controlled by parameters.

## Details

- **Entry Criteria**:
  - **Long**: Enabled long condition is true.
  - **Short**: Enabled short condition is true.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Enabled close long condition is true.
  - **Short**: Enabled close short condition is true.
- **Stops**: No.
- **Default Values**:
  - `LongOperator` = `>`
  - `CloseLongOperator` = `<`
  - `ShortOperator` = `<`
  - `CloseShortOperator` = `>`
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Simple
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
