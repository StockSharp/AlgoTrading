# MA With Logistic
[Русский](README_ru.md) | [中文](README_cn.md)

MA With Logistic is a moving average strategy that uses a fast and slow moving average for entries and supports percent or logistic-based exits.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close > fast MA and fast MA > slow MA.
  - **Short**: Close < fast MA and fast MA < slow MA.
- **Exit Criteria**: Percentage targets or logistic probability thresholds.
- **Stops**: Percentage-based or logistic probability exits.
- **Default Values**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: MA
  - Complexity: Low
  - Risk level: Medium
