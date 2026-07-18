# LSMA Fast And Simple Alternative Calculation Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses a fast approximation of the Least Squares Moving Average (LSMA) computed as `3 × WMA − 2 × SMA`. A long position is opened when the price crosses above the LSMA, and a short position is opened when the price crosses below it.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above LSMA.
  - **Short**: Close crosses below LSMA.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - Length 25.
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: WMA, SMA
  - Stops: No
  - Complexity: Simple
  - Timeframe: Not specified
