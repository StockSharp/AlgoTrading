# Volume Exhaustion Strategy

Sharp spikes in volume often signal the end of a move as traders rush to exit or enter positions. This strategy measures current volume against an average to spot exhaustion. When combined with candle direction and a moving average filter it can pinpoint reversal entries.

Each candle updates the average volume. If the new bar's volume exceeds this average by a set multiplier and the candle closes in the direction opposite the prevailing trend, the system enters a trade. A stop based on ATR protects the position.

The trade is typically exited via the stop-loss as the strategy anticipates a swift reversal following the volume burst.

## Details

- **Entry Criteria**: Volume spike above average with candle opposite the trend.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 2.0
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Volume, MA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
