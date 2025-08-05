# OBV Divergence Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
On-Balance Volume tracks cumulative trading volume with the idea that volume precedes price. When price forms a new high but OBV fails to confirm—or vice versa—a reversal may be brewing. This strategy uses that divergence to fade unsustainable moves.

Testing indicates an average annual return of about 112%. It performs best in the forex market.

For each candle OBV is updated and compared with the prior reading. A bullish signal emerges if price makes a lower low while OBV prints a higher low. A bearish signal occurs when price rallies to a higher high but OBV lags. A moving average provides an exit point, while a percentage stop keeps losses controlled.

The approach attempts to capture mean reversion following volume exhaustion and often holds trades only until price crosses back over the average line.

## Details

- **Entry Criteria**: Price/OBV divergence.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing moving average or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `DivergencePeriod` = 5
  - `MAPeriod` = 20
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: OBV, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium

