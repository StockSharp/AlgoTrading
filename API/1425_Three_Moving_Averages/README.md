# Three Moving Averages Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades when a short moving average crosses a medium one while both are aligned relative to a long-term average.

## Details

- **Entry Criteria**:
  - **Long**: Short MA crosses above medium MA and medium MA is above long MA.
  - **Short**: Short MA crosses below medium MA and medium MA is below long MA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `ShortMa` = 20
  - `MediumMa` = 50
  - `LongMa` = 200
