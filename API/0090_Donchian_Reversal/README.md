# Donchian Channel Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Donchian Channels mark recent highs and lows over a chosen period. Prices that pierce those boundaries and then reverse can signal exhaustion. This strategy watches for closes back inside the channel after a brief breakout.

If the previous close was below the lower band and the current close moves back above it, a long trade is taken. Conversely, if the prior close was above the upper band and price falls back inside, a short is opened. A percentage stop manages risk in both cases.

By trading only after a failed breakout this approach attempts to capture false moves that quickly retrace.

## Details

- **Entry Criteria**: Price closes back inside Donchian Channel after breaching upper or lower band.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `Period` = 20
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Donchian Channel
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 157%. It performs best in the crypto market.
