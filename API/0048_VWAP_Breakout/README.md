# VWAP Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
VWAP Breakout looks for price to cross the Volume Weighted Average Price from the opposite side. A breakout above VWAP signals bullish pressure, while a drop below VWAP signals bearish sentiment.

The strategy waits for a close on the other side of VWAP and then trades in that direction. Exits occur when price reverses back through VWAP.

Because VWAP represents the average transaction price, breaks often lead to momentum moves.

## Details

- **Entry Criteria**: Price closes on the opposite side of VWAP.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses back through VWAP or stop.
- **Stops**: Yes.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: VWAP
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
