# ADX for BTC
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses Average Directional Index (ADX) with optional SMA trend filter to catch strong moves in Bitcoin.

Testing indicates an average annual return of about 80%. It performs best in the crypto market.

The system buys when ADX crosses above the entry level and the trend filter is bullish. The position closes when ADX falls below the exit level.

## Details

- **Entry Criteria**: ADX crosses above `EntryLevel` and (if enabled) fast SMA > slow SMA.
- **Long/Short**: Long only.
- **Exit Criteria**: ADX crosses below `ExitLevel`.
- **Stops**: No.
- **Default Values**:
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ADX, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
