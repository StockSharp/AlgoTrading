# Vicious Mortgage Rates V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades a synthetic index built from four volatility measures.
A long position is opened when the fast EMA of the product crosses above the slow EMA, and a short position is opened on the opposite cross.

## Details

- **Entry Criteria**: fast EMA of combined index crosses above slow EMA
- **Long/Short**: Both
- **Exit Criteria**: opposite cross
- **Stops**: No
- **Default Values**:
  - `FastLength` = 8
  - `SlowLength` = 21
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
