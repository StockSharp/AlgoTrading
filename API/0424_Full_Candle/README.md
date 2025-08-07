# Full Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Full Candle setup enters when a candle closes beyond its EMA and leaves only a small wick on the breakout side. The intent is to trade momentum candles that show decisive action without much rejection. Optional percentage based take-profit and stop-loss exits manage the trade once it is open.

The system is best suited for short-term breakouts where strong candles often lead to quick follow-through.

## Details

- **Entry Criteria**:
  - **Long**: bullish candle closing above EMA with shadow ≤ threshold
  - **Short**: bearish candle closing below EMA with shadow ≤ threshold
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Take-profit or stop-loss percentages if enabled
- **Stops**: Optional
- **Default Values**:
  - `EmaLength` = 10
  - `ShadowPercent` = 5
  - `TPPercent` = 1.2
  - `SLPercent` = 1.8
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: EMA, price action
  - Stops: Optional
  - Complexity: Low
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
