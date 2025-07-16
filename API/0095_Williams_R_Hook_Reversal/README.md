# Williams %R Hook Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Williams %R Hook Reversal follows the Williams %R indicator as it quickly snaps back from an extreme.
When the reading moves above -20 or below -80 and then hooks toward the center, the prior thrust is likely exhausted.

The strategy buys when %R reverses higher from oversold while price presses new lows and sells when it hooks downward from overbought during new highs.

A tight percent stop controls risk and trades exit once %R hooks in the opposite direction or the stop triggers.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Williams %R
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
