# Hammer + EMA Strategy with Tick-based SL/TP
[Русский](README_ru.md) | [中文](README_cn.md)

Combines hammer and inverted hammer candlestick patterns with an EMA trend filter and tick-based risk management.

## Details

- **Entry Criteria**: Hammer above EMA or inverted hammer below EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Tick-based take profit or stop loss.
- **Stops**: Tick-based.
- **Default Values**:
  - `EmaLength` = 50
  - `StopLossTicks` = 1
  - `TakeProfitTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: EMA, Hammer, Inverted Hammer
  - Stops: Tick-based
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
