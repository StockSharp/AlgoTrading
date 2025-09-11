# Squeeze Momentum Indicator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Squeeze Momentum Indicator strategy detects volatility contraction when Bollinger Bands fall inside Keltner Channels. A long position is opened when the squeeze releases upward with rising momentum and price above the 100-period EMA. Shorts are taken on a downward release with falling momentum and price below the EMA. Positions exit when momentum reverses.

## Details

- **Entry Criteria**:
  - Bollinger Bands move outside Keltner Channels (squeeze release).
  - **Long**: Momentum increases, price above previous close and EMA100, and squeeze color changes from black to gray.
  - **Short**: Momentum decreases, price below previous close and EMA100, and color changes from gray to black.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Momentum reverses.
- **Stops**: None.
- **Default Values**:
  - `BbLength` = 20
  - `BbMultiplier` = 2
  - `KcLength` = 20
  - `KcMultiplier` = 1.5
  - `EmaLength` = 100
- **Filters**:
  - Category: Volatility breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Keltner Channels, Linear Regression, EMA
  - Stops: None
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
