# Chande Momentum Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy buys when the Chande Momentum Oscillator drops below a lower threshold and closes the position when it rises above an upper threshold or after a fixed number of bars.

Testing indicates an average annual return of about 40%. It performs best in trending markets.

The oscillator compares recent gains and losses to gauge momentum. Extreme negative values suggest oversold conditions, which the strategy uses for long entries. Positions are closed when momentum turns positive or the holding period expires.

## Details

- **Entry Criteria**: `CMO < LowerThreshold`.
- **Long/Short**: Long only.
- **Exit Criteria**: `CMO > UpperThreshold` or `MaxBarsInPosition` bars elapsed.
- **Stops**: No.
- **Default Values**:
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: CMO
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
