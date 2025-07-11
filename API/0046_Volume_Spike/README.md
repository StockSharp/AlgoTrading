# Volume Spike Trend

Volume Spike Trend monitors sudden surges in traded volume. When current volume exceeds the recent average by a set multiplier, it signals strong participation.

If volume spikes and price is above the moving average, the strategy buys; if volume spikes below the average, it sells short. Trades exit when volume falls back under the average or the stop-loss is reached.

This method seeks to catch moves fueled by a burst of activity.

## Rules

- **Entry Criteria**: Volume change exceeds `VolumeSpikeMultiplier` times average.
- **Long/Short**: Both directions.
- **Exit Criteria**: Volume drops below average or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `VolAvgPeriod` = 20
  - `VolumeSpikeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume, MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
