# ColorSchaff JJRSX Trend Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies the Schaff Trend Cycle oscillator based on JJRSX averages. It opens long or short positions when the oscillator crosses user-defined levels.

## Details

- **Entry Criteria**:
  - Buy when the Schaff Trend Cycle crosses above `HighLevel`. Any short position is closed first.
  - Sell when the Schaff Trend Cycle crosses below `LowLevel`. Any long position is closed first.
- **Long/Short**: Both.
- **Exit Criteria**: Positions close when an opposite entry signal occurs.
- **Stops**: None.
- **Default Values**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
