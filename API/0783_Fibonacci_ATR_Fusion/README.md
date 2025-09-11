# Fibonacci ATR Fusion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines buying pressure ratios across multiple Fibonacci periods with ATR and uses threshold crosses for entries and exits. Optional ATR-based layered take profit.

## Details

- **Entry Criteria**:
  - **Long**: Weighted average crosses above `LongEntryThreshold`.
  - **Short**: Weighted average crosses below `ShortEntryThreshold`.
- **Exit Criteria**:
  - Weighted average crosses opposite exit thresholds or position reversal.
- **Indicators**:
  - Weighted buying pressure ratios over ATR.
  - ATR for optional take profit.
- **Stops**: None.
- **Default Values**:
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: ATR
  - Stops: none
  - Complexity: Medium
