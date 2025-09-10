# Candle Body Shapes Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades based on where a candle opens and closes within its range.
Enters long when the candle opens near its low and closes near its high, showing strong bullish pressure.
Enters short when the candle opens near its high and closes near its low, indicating strong bearish pressure.

The approach relies purely on price action and can be applied to any liquid market.

## Details

- **Entry Criteria**:
  - Long: `Open near Low && Close near High`
  - Short: `Open near High && Close near Low`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `BodyThreshold` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Candlestick pattern
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
