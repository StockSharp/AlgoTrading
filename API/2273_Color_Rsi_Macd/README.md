# Color RSI MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades signals from a MACD indicator that can be analysed in four different modes:

- **Breakdown** – trade when the MACD histogram crosses the zero line.
- **MACD Twist** – trade when the MACD line changes direction.
- **Signal Twist** – trade when the signal line changes direction.
- **MACD Disposition** – trade on crossings between the MACD line and the signal line.

Each mode can independently open or close long and short positions using the corresponding flags.

No stop-loss or take-profit levels are used by default.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `CandleType` = 4-hour
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
