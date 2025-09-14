# ColorJFatl Digit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the slope direction of a Jurik Moving Average (JMA) to generate trades. The JMA approximates the "ColorJFatl_Digit" indicator from the original MQL5 expert. A long position is opened when the JMA turns upward, while a short position is opened when the JMA turns downward. Opposite positions are closed when the slope reverses.

The system trades both directions and does not employ hard stops by default. It suits instruments where trend changes can be captured by a smooth adaptive moving average.

## Details

- **Entry Criteria**:
  - **Long**: JMA slope changes from negative to positive.
  - **Short**: JMA slope changes from positive to negative.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: JMA slope turns negative.
  - **Short**: JMA slope turns positive.
- **Stops**: None by default.
- **Default Values**:
  - `JMA Length` = 5
  - `Timeframe` = 4 hours
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
