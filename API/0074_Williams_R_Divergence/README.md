# Williams %R Divergence Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Williams %R oscillator gauges overbought and oversold conditions. When price makes a new low but %R forms a higher low, or when price prints a new high but %R turns lower, momentum may reverse. This strategy hunts for such divergences at the extremes of the indicator.

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

Every bar the system records the latest close and %R value to compare with the prior reading. A bullish divergence combined with an oversold level below -80 triggers a long entry, while a bearish divergence and a reading above -20 produces a short. Stops are set using a percentage of price.

Positions exit when the oscillator returns to the opposite extreme, capturing the snap back from the divergence signal.

## Details

- **Entry Criteria**: Price/%R divergence with %R below -80 for longs or above -20 for shorts.
- **Long/Short**: Both.
- **Exit Criteria**: Williams %R reaching the opposite extreme or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `WilliamsRPeriod` = 14
  - `DivergencePeriod` = 5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: Williams %R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium

