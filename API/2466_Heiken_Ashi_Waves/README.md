# Heiken Ashi Waves Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy blending Heikin-Ashi candles with a dual moving average wave filter. The fast SMA (2) crossing the slow SMA (30) signals potential wave changes and is confirmed by the current Heikin-Ashi candle direction.

## Details

- **Entry Criteria**:
  - Long: bullish Heikin-Ashi candle and fast SMA crossing above slow SMA
  - Short: bearish Heikin-Ashi candle and fast SMA crossing below slow SMA
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite cross
  - Trailing stop loss
- **Stops**: Trailing stop in points via `StopLoss`
- **Default Values**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: Heikin Ashi, SMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
