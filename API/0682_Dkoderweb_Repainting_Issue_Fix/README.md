# Dkoderweb Repainting Issue Fix Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects harmonic patterns using a simple zigzag approach and trades when price returns to a Fibonacci retracement level. When a bullish pattern forms and price pulls back to the entry window, the strategy opens a long position with predefined take‑profit and stop‑loss levels. A bearish pattern triggers the same logic in the opposite direction.

## Details

- **Entry Criteria**:
  - **Long**: ABCD harmonic pattern and close price at or below the entry Fibonacci level.
  - **Short**: ABCD harmonic pattern and close price at or above the entry Fibonacci level.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Price reaches take‑profit or stop‑loss Fibonacci levels.
- **Stops**: Yes.
- **Default Values**:
  - `TradeSize` = 1
  - `EntryRate` = 0.382
  - `TakeProfitRate` = 0.618
  - `StopLossRate` = -0.618
- **Filters**:
  - Category: Pattern recognition
  - Direction: Both
  - Indicators: ZigZag
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium

