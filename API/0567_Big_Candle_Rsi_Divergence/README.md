# Big Candle RSI Divergence
[Русский](README_ru.md) | [中文](README_cn.md)

Identifies unusually large candles relative to the prior five bars and compares fast and slow RSI values. Trades follow the candle direction and use a delayed trailing stop that activates only after price moves a set number of ticks in profit.

The trailing stop begins once the profit threshold is reached and then tracks price at a fixed distance, while an initial fixed stop protects the trade from the start.

## Details

- **Entry Criteria**:
  - **Long**: Current candle body bigger than the previous five and closes up.
  - **Short**: Current candle body bigger than the previous five and closes down.
- **Long/Short**: Both directions.
- **Exit Criteria**: Initial stop or trailing stop hit.
- **Stops**: Yes, delayed trailing stop.
- **Default Values**:
  - `TrailStartTicks` = 200
  - `TrailDistanceTicks` = 150
  - `InitialStopLossTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
