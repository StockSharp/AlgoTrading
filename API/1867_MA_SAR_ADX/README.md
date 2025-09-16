# MA SAR ADX Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Moving Average, Parabolic SAR and Average Directional Index (ADX).
Buys when the price is above both the moving average and SAR while +DI is above -DI.
Sells when the price is below both the moving average and SAR while +DI is below -DI.
Positions are closed when price crosses the SAR.

## Details

- **Entry Criteria**:
  - Long: `Close > MA && +DI >= -DI && Close > SAR`
  - Short: `Close < MA && +DI <= -DI && Close < SAR`
- **Long/Short**: Both
- **Exit Criteria**: Price crosses Parabolic SAR
- **Stops**: No
- **Default Values**:
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, Parabolic SAR, ADX
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
