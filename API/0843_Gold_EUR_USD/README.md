# Gold & EUR/USD Liquidity Grab Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects liquidity grabs at supply and demand zones on Gold and EUR/USD using RSI, SMA, Stochastic Oscillator and ATR-based fair value gaps.

## Details

- **Entry Criteria**:
  - **Long**: Price wicks below the recent low, market structure shifts up, fair value gap occurs, RSI oversold, price above SMA, Stochastic oversold.
  - **Short**: Price wicks above the recent high, market structure shifts down, fair value gap occurs, RSI overbought, price below SMA, Stochastic overbought.
- **Long/Short**: Both sides.
- **Exit Criteria**: Reverse signal.
- **Stops**: No.
- **Default Values**:
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: RSI, SMA, Stochastic, ATR, Highest, Lowest
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
