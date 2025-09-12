# NQ Phantom Scalper Pro Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

VWAP band breakout strategy with optional volume and trend filters.

## Details

- **Entry Criteria**:
  - **Long**: Price closes above the upper VWAP band with confirming volume.
  - **Short**: Price closes below the lower VWAP band with confirming volume.
- **Long/Short**: Both
- **Exit Criteria**:
  - Price crosses back through VWAP or the ATR stop is hit.
- **Stops**: ATR-based
- **Default Values**:
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: VWAP, ATR, EMA, SMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
