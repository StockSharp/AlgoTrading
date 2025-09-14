# Fine Tuning MA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors the slope of a simple moving average. After two consecutive bars in one direction, a reversal of the moving average triggers an entry. A rising turn after a decline opens a long position while a falling turn after an ascent opens a short. Opposite signals close existing trades.

The system was converted from the MQL "Exp_FineTuningMA" expert and replaces the original custom indicator with a standard simple moving average for clarity.

## Details

- **Entry Criteria**: MA changes direction after two bars.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes, percent based.
- **Default Values**:
  - `MaLength` = 10
  - `TakeProfitPercent` = 1
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Swing / H4
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
