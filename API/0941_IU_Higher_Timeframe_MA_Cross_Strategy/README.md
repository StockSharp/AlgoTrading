# IU Higher Timeframe MA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

IU Higher Timeframe MA Cross Strategy trades when a fast moving average computed on a user-selected timeframe crosses a slower moving average from possibly another timeframe. A long position opens on a bullish cross and a short position on a bearish cross. Stop loss is placed at the previous candle's extreme, and take profit uses a configurable risk-to-reward ratio.

## Details
- **Data**: Candles from specified timeframes.
- **Entry Criteria**:
  - **Long**: MA1 crosses above MA2.
  - **Short**: MA1 crosses below MA2.
- **Exit Criteria**: Stop loss or take profit hit.
- **Stops**: Previous candle high/low with `RiskToReward` multiplier.
- **Default Values**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: Moving Average
  - Complexity: Low
  - Risk level: Medium
