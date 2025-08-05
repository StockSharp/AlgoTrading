# Donchian Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Donchian Volume uses Donchian channel breakouts confirmed by rising volume to initiate trades.
A move outside the channel on strong volume suggests the start of a new trend.

Testing indicates an average annual return of about 160%. It performs best in the forex market.

The strategy enters in the direction of the breakout and exits when price closes back inside the channel or volume wanes.

Stops are set a short distance inside the channel to protect against false moves.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Donchian Channel, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

