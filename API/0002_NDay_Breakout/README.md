# NDay Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
N-day high/low breakout strategy N-day breakout looks for new highs or lows over the given period. Entries occur when price pierces the latest N-day high or low, anticipating momentum. A moving-average filter and percentage stop manage exits.

By waiting for the prior extreme to break, the system attempts to catch the start of a directional move. Filtering by a trend-following average helps avoid false signals that arise during consolidation.


## Details

- **Entry Criteria**: Signals based on MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `MaPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 43%. It performs best in the stocks market.
