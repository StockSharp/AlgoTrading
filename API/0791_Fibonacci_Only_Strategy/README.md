# Fibonacci-Only Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Fibonacci-Only Strategy uses custom 19% and 82.56% retracement levels derived from the last 100 candles. The strategy enters when price touches or breaks these levels with confirmation from candle direction. It supports optional breakout entries, ATR-based stop loss, trailing stop and seven stacked take profits.

## Details

- **Entry Criteria**: touch or break of Fibonacci levels with confirmation
- **Long/Short**: Both
- **Exit Criteria**: stop loss, trailing stop, or take profit targets
- **Stops**: ATR or percent
- **Default Values**:
  - `CandleType` = 15 minutes
- **Filters**:
  - Category: Fibonacci
  - Direction: Both
  - Indicators: Highest, Lowest, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
