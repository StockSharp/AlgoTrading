# Parabolic SAR Early Buy MA Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades Parabolic SAR reversals and exits long positions early when SAR flips above price and the close is below an N-period moving average.

## Details

- **Entry Criteria**:
  - Price crossing Parabolic SAR.
- **Long/Short**: Both.
- **Exit Criteria**:
  - For long positions: SAR above price and close below MA (`MaPeriod`).
  - For short positions: opposite SAR crossover (handled by entry logic).
- **Stops**: None.
- **Default Values**:
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: Parabolic SAR, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
