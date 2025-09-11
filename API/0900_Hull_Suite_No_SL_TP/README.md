# Hull Suite No SL/TP
[Русский](README_ru.md) | [中文](README_cn.md)

Hull Suite No SL/TP is a trend-following strategy based on Hull Moving Average variations. It flips position when the hull line changes direction compared to two candles ago.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Hull value is greater than two candles ago.
  - **Short**: Hull value is less than two candles ago.
- **Exit Criteria**: Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `Length` = 55
  - `Mode` = `Hma`
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: Hull Moving Average
  - Complexity: Low
  - Risk level: Low
