# Projection Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes the average percentage change of recent daily opens and projects breakout levels around the current day's open. Long positions are entered when price breaks above the upper projection, while short positions are opened on a break below the lower projection. Protective stops are placed near the opposite side of the projection.

## Details

- **Entry Criteria**:
  - **Long**: price crosses above `open + threshold`.
  - **Short**: price crosses below `open - threshold`.
- **Exit Criteria**:
  - **Long**: price falls below the long stop.
  - **Short**: price rises above the short stop.
- **Stops**: yes, based on average change.
- **Parameters**:
  - `TargetMultiple` – multiplier for the average change (default 0.2).
  - `Threshold` – percentage of the average change used to form breakout levels (default 1.0).
  - `CalculationPeriod` – number of days in the average (default 5).
