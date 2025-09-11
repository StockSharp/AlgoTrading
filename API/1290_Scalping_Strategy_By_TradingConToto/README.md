# Scalping Strategy By TradingConToto
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping Strategy by TradingConToto draws lines between consecutive pivot highs or lows depending on EMA trend. When price crosses above a descending pivot-high line during an uptrend, the strategy enters long. When price falls below an ascending pivot-low line during a downtrend, it enters short. Trading is allowed only during a specified session.

## Details

- **Entry Criteria**: Uptrend with price breaking a descending pivot-high line for long; downtrend with price breaking an ascending pivot-low line for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: Take profit and stop loss.
- **Stops**: Yes.
- **Default Values**:
  - `Pivot` = 16
  - `Pips` = 64
  - `Spread` = 0
  - `Session` = "0830-0930"
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, pivot
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
