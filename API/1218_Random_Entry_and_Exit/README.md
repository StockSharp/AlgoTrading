# Random Entry and Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses random numbers to enter and exit positions. For each finished candle a random value between 0 and 1 is generated. If the value is below the entry threshold, a trade is opened. Another random value controls exits. Long and short trades can be enabled separately.

## Details

- **Entry Criteria**: random value < Entry Threshold.
- **Exit Criteria**: random value < Exit Threshold.
- **Long/Short**: Both, individually configurable.
