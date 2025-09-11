# Efficient Work Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses moving averages on short, medium, and long horizons. A long position is opened when the fast average is above both longer averages, and a short position is opened when it is below them.

## Details

- **Entry Criteria**:
  - **Long**: `fast MA > medium MA` and `fast MA > high MA`.
  - **Short**: `fast MA < medium MA` and `fast MA < high MA`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal triggers a reversal.
- **Stops**: None.
- **Default Values**:
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
