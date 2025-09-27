# Global Index Spread RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Global Index Spread RSI Strategy trades the E-mini S&P 500 when its spread to a global equity index becomes oversold. The spread is measured in percentage terms and passed through a short-length RSI. A long position opens when the RSI falls below the oversold threshold and closes when it rises above the overbought threshold.

## Details
- **Data**: Daily closes of ES and global index.
- **Entry Criteria**:
  - **Long**: Spread RSI below `OversoldThreshold`.
- **Exit Criteria**: Spread RSI above `OverboughtThreshold`.
- **Stops**: None.
- **Default Values**:
  - `RsiLength` = 2
  - `OversoldThreshold` = 35
  - `OverboughtThreshold` = 78
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: RSI
  - Complexity: Low
  - Risk level: Medium
