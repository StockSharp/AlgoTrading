# Go Candle Body Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Go indicator that averages candle body size. It opens a long position when the smoothed candle body crosses below zero after being positive and opens a short position on the opposite cross. Existing positions are closed on opposite signals.

## Details

- **Entry Criteria**: sign change of body SMA (positive to negative for long, negative to positive for short)
- **Long/Short**: Both
- **Exit Criteria**: opposite sign change of body SMA
- **Stops**: No
- **Default Values**:
  - `Period` = 174
  - `CandleType` = 1 hour
- **Filters**:
  - Category: Reversal
  - Direction: Long & Short
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
