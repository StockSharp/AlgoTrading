# EMA50 Crossover Monthly DCA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

EMA50 Crossover Monthly DCA buys when price closes above the 50-period EMA and accumulates additional positions each month. Uninvested DCA amounts are stored as cash and deployed once the trend resumes.

The strategy sells when price falls below the EMA, exiting the position.

## Details

- **Entry Criteria**: close > EMA(50)
- **Long/Short**: Long only
- **Exit Criteria**: price crosses below EMA(50)
- **Stops**: No
- **Default Values**:
  - `CandleType` = 1 week
  - `DcaAmount` = 100000
  - `StartDate` = 1980-01-01
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Long-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
