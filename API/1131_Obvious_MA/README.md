# OBVious MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters a long position when OBV crosses above its long entry moving average and exits when OBV crosses below the long exit average. Short positions are entered when OBV crosses below its short entry average and exited when it crosses above the short exit average. A direction filter allows enabling only long or short trades.

## Details

- **Entry Criteria**:
  - **Long**: OBV crosses above long entry MA and direction is not Short.
  - **Short**: OBV crosses below short entry MA and direction is not Long.
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: OBV crosses below long exit MA.
  - Short: OBV crosses above short exit MA.
- **Stops**: None.
- **Default Values**:
  - `LongEntryLength` = 190
  - `LongExitLength` = 202
  - `ShortEntryLength` = 395
  - `ShortExitLength` = 300
  - `TradeDirection` = "Long"
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: OBV, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
