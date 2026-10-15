# Dark Pool Prints Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dark Pool Prints tracks large off-exchange transactions that often precede sharp moves once the activity is revealed.
Unusual volume hitting the tape can signal institutional positioning that hasn't yet impacted the regular market.

Testing indicates an average annual return of about 46%. It performs best in the stocks market.

The strategy enters in the same direction as heavy dark pool buying or selling, expecting follow-through when the rest of the market reacts.

A small percent stop keeps risk contained and positions close if the anticipated momentum fails to appear.

### Executed trading rules (C# and Python)

At the default one-minute candle interval, the 20-period SMA confirms direction. After 20 finished volume samples have been collected, the current volume must be **strictly greater** than 1.5 times the arithmetic mean of the preceding 19 samples. Only a flat strategy may enter: buy on a bullish candle (`Close > Open`) above SMA20; sell on a bearish candle (`Close < Open`) below SMA20. Cooldown is 500 finished bars between discretionary entries. A held long exits when `Close < SMA20`, and a held short exits when `Close > SMA20`; these exits **do not depend on a new volume spike or an expired entry cooldown**. Each filled position has native 2% stop-loss protection (no take-profit). The published return estimate above is not established by this example's BTC/TON replay.


## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - \`CandleType\` = 1 minute
  - `StopLossPercent` = 2%
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

