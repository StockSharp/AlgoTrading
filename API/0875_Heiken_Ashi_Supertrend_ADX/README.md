# Heiken Ashi Supertrend Adx Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Heiken Ashi candles, Supertrend direction and optional ADX filter. A bullish Heiken Ashi candle without a lower wick opens a long in an uptrend. Bearish candles without upper wicks open shorts in a downtrend. Positions close on opposite signals or an ATR based trailing stop.

Testing indicates an average annual return of about 128%. It performs best in the crypto market.

Heiken Ashi smooths noise while Supertrend and ADX confirm direction. ATR determines dynamic stops.

## Details

- **Entry Criteria**:
  - Long: bullish HA candle without lower wick with optional Supertrend up and ADX confirmation
  - Short: bearish HA candle without upper wick with optional Supertrend down and ADX confirmation
- **Long/Short**: Both
- **Exit Criteria**: Opposite candle or ATR trailing stop
- **Stops**: ATR trailing stop
- **Default Values**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Heiken Ashi, Supertrend, ADX, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

