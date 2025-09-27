# ProfitView Strategy Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A basic moving average crossover strategy with configurable smoothing types, derived from the ProfitView template.

## Details

- **Entry Criteria**:
  - **Long**: MA1 crosses above MA2.
  - **Short**: MA1 crosses below MA2.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `MA1 Type` = SMA, `MA1 Length` = 10
  - `MA2 Type` = SMA, `MA2 Length` = 100
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving averages
  - Stops: No
  - Complexity: Basic
