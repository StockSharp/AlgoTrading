# Color Zerolag RVI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Relative Vigor Index and its signal line.
It buys when the main RVI line crosses below the signal line and sells when the main line crosses above the signal line.

## Details

- **Entry Criteria**: Cross of RVI and signal line
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4 hours
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RVI, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (H4)
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
