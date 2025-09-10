# Averaging Down Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Averaging Down strategy buys when the Relative Strength Index (RSI) drops below a defined threshold. Each signal adds to the existing long position, averaging the entry price. The strategy exits when the closing price breaks above the previous bar's high.

## Details

- **Entry Criteria**:
  - RSI below `RsiBuyThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close price exceeds the previous bar's high.
- **Stops**: None.
- **Default Values**:
  - `RsiLength` = 10
  - `RsiBuyThreshold` = 33
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
