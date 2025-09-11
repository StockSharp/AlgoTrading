# Smart Grid Scalping Pullback Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid-based scalping strategy that expands ATR-driven price levels from a base price twenty bars back. Pullbacks are filtered with RSI before entries. Positions use a profit target and an ATR trailing stop.

## Details

- **Entry Criteria**:
  - Long: close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - Short: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **Long/Short**: Both
- **Exit Criteria**: profit target or ATR trailing stop
- **Stops**: ATR trailing stop
- **Default Values**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: ATR, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
