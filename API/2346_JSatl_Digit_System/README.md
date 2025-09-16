# JSatl Digit System
[Русский](README_ru.md) | [中文](README_cn.md)

The JSatl Digit System uses a Jurik Moving Average (JMA) to determine trend direction.
The strategy measures the slope of the JMA and opens a position when price confirms the slope direction.

A long position is opened when the JMA is rising and the close price is above the average.
A short position is opened when the JMA is falling and the close price is below the average.
Opposite signals close any open position.

## Details

- **Entry Criteria**: JMA slope with price confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `JmaLength` = 14
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: JMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Swing (4h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
