# Ride Alligator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implementation of the Ride Alligator strategy. The method uses three moving averages known as the Alligator indicator. A long position is opened when the Lips line crosses above the Jaws line while the Teeth line is below the Jaws. A short position is opened when the Lips cross below the Jaws and the Teeth line is above the Jaws. The open position is protected by a stop at the Jaws line which trails as the line moves.

## Details

- **Entry Criteria**:
  - Long: `Lips > Jaws && Teeth < Jaws && previous Lips < previous Jaws`
  - Short: `Lips < Jaws && Teeth > Jaws && previous Lips > previous Jaws`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `price <= Jaws`
  - Short: `price >= Jaws`
- **Stops**: Trailing stop at Alligator Jaws
- **Default Values**:
  - `AlligatorPeriod` = 5
  - `MaType` = MovingAverageTypeEnum.Weighted
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Alligator
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
