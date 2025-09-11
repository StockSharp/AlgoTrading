# Engulfing & Pin Bar Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy waits for a hammer or bullish engulfing candle and enters long on a breakout above the signal high. For bearish setups, it uses shooting star or bearish engulfing and sells on a break below the signal low. Stop loss is placed at the opposite side of the signal candle and take profit uses a risk/reward ratio.

## Details

- **Entry Criteria:** hammer or bullish engulfing followed by breakout above high; shooting star or bearish engulfing followed by breakout below low.
- **Long/Short:** Both.
- **Exit Criteria:** stop at opposite side of signal candle; take profit at multiple of risk.
- **Stops:** Yes.
- **Default Values:**
  - Long profit ratio = 5
  - Short profit ratio = 4
  - Risk percent = 0.02
  - Candle timeframe = 1 minute
- **Filters:**
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
