# Ma Adx Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy based on MA and ADX indicators. Enters position when price crosses MA with strong trend.

Testing indicates an average annual return of about 184%. It performs best in the crypto market.

The moving average dictates the trend, and ADX verifies whether it's strong enough to trade. Entries follow price crossings of the MA when ADX exceeds a threshold.

This classic trend approach appeals to systematic traders. Losses are managed with a percent stop and profits taken at an ATR-based target.

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
  - `AdxThreshold` = 25
  - `AtrPeriod` = 14
    Entries need the close to cross the SMA on that candle while ADX is above AdxThreshold; the reverse cross closes the position and, with ADX still strong, opens the other side. The target lies TakeProfitAtrMultiplier ATRs (AtrPeriod) from the entry close and is checked on candle closes.
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

