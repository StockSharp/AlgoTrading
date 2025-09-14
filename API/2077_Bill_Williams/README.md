# Bill Williams
[Русский](README_ru.md) | [中文](README_cn.md)

Bill Williams combines the Alligator indicator with fractal breakouts. The jaws, teeth and lips must diverge before a breakout of the most recent fractal triggers an order.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - Calculate fractal highs and lows from the last 5 candles.
  - The distance between Jaw and Teeth must exceed `GatorDivSlowPoints`.
  - The distance between Lips and Teeth must exceed `GatorDivFastPoints`.
  - **Long**: Price closes above the last up fractal by at least `FilterPoints` points and the candle is bullish.
  - **Short**: Price closes below the last down fractal by at least `FilterPoints` points and the candle is bearish.
- **Exit Criteria**:
  - Opposite breakout.
  - Trailing stop at the latest opposite fractal.
- **Stops**: Fractal-based trailing stop.
- **Default Values**:
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = 1 hour candles
