# VRS Vegas Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Reversal strategy using candle wicks.

Testing indicates an average annual return of about 37%. It performs best in the crypto market.

The system looks for large spikes relative to the close price. A large lower wick triggers a long entry while a large upper wick triggers a short. Positions are closed when price moves twice the spike size in profit.

## Details

- **Entry Criteria**:
  - **Long**: lower wick ≥ Spike% * close and no upper spike.
  - **Short**: upper wick ≥ Spike% * close and no lower spike.
- **Long/Short**: Both sides.
- **Exit Criteria**: Target at entry ± (spike * 2).
- **Stops**: No.
- **Default Values**:
  - `SpikePercent` = 0.025
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Price action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
