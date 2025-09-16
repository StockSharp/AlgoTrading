# Delta MFI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on comparing fast and slow Money Flow Index (MFI) values. It goes long when the fast MFI rises above the slow MFI while the slow MFI is above the signal level. It goes short when the fast MFI falls below the slow MFI while the slow MFI is below 100 minus the signal level.

## Details

- **Entry Criteria**: 
  - Buy when `slow MFI > Level` and `fast MFI > slow MFI`
  - Sell when `slow MFI < 100 - Level` and `fast MFI < slow MFI`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 50
  - `Level` = 50
  - `CandleType` = 4-hour candles
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: Money Flow Index
  - Stops: No
  - Complexity: Basic
  - Timeframe: H4
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
