# Fibonacci TP SL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses Fibonacci retracement levels to generate entries with ATR-based stop-loss and percentage take-profit. Trading is limited by a minimum bar gap between trades and a weekly profit cap.

## Details

- **Entry Criteria**:
  - **Long**: `Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `Min bars since last trade`
  - **Short**: `Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `Min bars since last trade`
- **Long/Short**: Both sides
- **Exit Criteria**:
  - `ATR stop-loss` or `Take-profit`
- **Stops**: Yes
- **Default Values**:
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Highest, Lowest, ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
