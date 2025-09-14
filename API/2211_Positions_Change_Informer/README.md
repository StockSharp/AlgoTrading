# Positions Change Informer
[Русский](README_ru.md) | [中文](README_cn.md)

Positions Change Informer strategy monitors executed trades and notifies when the net position changes. It reports openings, closings and reversals, producing alerts as log entries, sounds or emails depending on the selected parameters. The strategy itself does not place orders and can be used alongside other trading systems.

## Details

- **Entry Criteria**: none (reacts to executed trades)
- **Long/Short**: Both
- **Exit Criteria**: none; notifications only
- **Stops**: No
- **Default Values**:
  - `Alert` = Alert
  - `SoundName` = "alert.wav"
  - `Language` = Russian
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: None

