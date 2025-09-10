# BB Breakout Momentum Squeeze Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The BB Breakout Momentum Squeeze strategy combines a Bollinger Breakout oscillator with a volatility squeeze filter. The squeeze is detected when Bollinger Bands move outside Keltner Channels, signaling potential expansion. A long trade occurs when the bullish breakout oscillator crosses above the threshold during this expansion, while a short trade uses the bearish crossing. Stops are based on an ATR band and a risk‑reward target completes the exit logic.

## Details

- **Entry Criteria**:
  - Squeeze off (Bollinger Bands outside Keltner Channels).
  - **Long**: Bull oscillator crosses above threshold.
  - **Short**: Bear oscillator crosses above threshold.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Price hits ATR stop or risk‑reward target.
- **Stops**: ATR band with risk‑reward target.
- **Default Values**:
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **Filters**:
  - Category: Volatility breakout
  - Direction: Both
  - Indicators: Bollinger Bands, Keltner Channels, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
