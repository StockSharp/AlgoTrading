# Fibonacci Auto Trend Scouter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses two rolling extremes based on Fibonacci numbers to scout emerging trends. The short window (8) tracks recent highs and lows while the long window (21) provides context. A long position is opened when the short-term high exceeds the long-term high. A short position opens when the short-term low falls below the long-term low.

## Details

- **Entry Criteria**:
  - **Long**: short-term high > long-term high.
  - **Short**: short-term low < long-term low.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Position is reversed on opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Short period` = 8
  - `Long period` = 21
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium-term
