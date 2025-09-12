# Tick Data Detailed Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Collects and aggregates tick volumes into multiple predefined ranges for both buy and sell directions. Useful for detailed tape reading without generating trading signals.

## Details

- **Entry Criteria**: None
- **Long/Short**: None
- **Exit Criteria**: None
- **Stops**: No
- **Default Values**:
  - `VolumeLessThan` = 10000
  - `Volume2From` = 10000
  - `Volume2To` = 20000
  - `Volume3From` = 20000
  - `Volume3To` = 50000
  - `Volume4From` = 50000
  - `Volume4To` = 100000
  - `Volume5From` = 100000
  - `Volume5To` = 200000
  - `Volume6From` = 200000
  - `Volume6To` = 400000
  - `VolumeGreaterThan` = 400000
- **Filters**:
  - Category: Volume
  - Direction: None
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Tick
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
