# Revelations Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A volatility breakout strategy that enters on strong ATR spikes confirmed by local extrema and a regime index. Position size adapts to spike strength.

## Details

- **Entry Criteria**:
  - **Long**: ATR spike upward at local low with regime confirmation.
  - **Short**: ATR spike downward at local high with regime confirmation.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Take profit or stop loss hit.
- **Stops**: Fixed percent stops.
- **Default Values**:
  - `ATR Fast` = 14
  - `ATR Slow` = 21
  - `ATR StdDev` = 12
  - `Spike Threshold` = 0.5
  - `Super Spike Mult` = 1.5
  - `Regime Window` = 8
  - `Regime Events` = 3
  - `Local Window` = 3
  - `Max Quantity` = 2
  - `Min Quantity` = 1
  - `Stop %` = 0.9
  - `Take Profit %` = 1.8
- **Filters**:
  - Category: Volatility breakout
  - Direction: Long/Short
  - Indicators: ATR, SMA, Highest/Lowest
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
