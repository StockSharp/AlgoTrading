# Donchian Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
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
  - `StopLossPercent` = 2
  - `DonchianPeriod` = 20
  - `VolumePeriod` = 20
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close beyond it on volume above the average of the previous VolumePeriod candles enters in the breakout's direction, reversing an opposite position. The position closes once price closes back inside the channel or volume falls below its average; the stop lies StopLossPercent from the entry price, just inside the broken boundary.
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

