# Ma Adx Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on MA and ADX indicators. Enters position when price crosses MA with strong trend.

The moving average dictates the trend, and ADX verifies whether it's strong enough to trade. Entries follow price crossings of the MA when ADX exceeds a threshold.

This classic trend approach appeals to systematic traders. Losses are managed with an ATR-based stop.

## Details

- **Entry Criteria**:
  - Long: `Close > MA && ADX > 25`
  - Short: `Close < MA && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: Reverse MA cross or stop
- **Stops**: `StopLossPercent` percent with take profit `TakeProfitAtrMultiplier` ATR
- **Default Values**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Moving Average, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 184%. It performs best in the crypto market.
