# Enhanced Doji Candle
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades doji candles with simple confirmation rules and fixed risk-reward management. It enters when a doji appears and the candle or its predecessor confirms direction by closing beyond the open with small wicks. Protective orders use a stop loss in pips and a take profit defined by a risk-reward ratio.

## Details

- **Entry Criteria**: Doji candle (body <= 30% of range). If bullish with lower wick <=1% or previous candle bullish, go long. If bearish with upper wick <=1% or previous candle bearish, go short.
- **Long/Short**: Both.
- **Exit Criteria**: Take-profit or stop-loss, or any new doji that closes the position.
- **Stops**: Yes.
- **Default Values**:
  - `RiskRewardRatio` = 2.0m
  - `StopLossPips` = 5
  - `SmaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Candlestick
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
