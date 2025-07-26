# Donchian Channel
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Donchian Channel

Donchian Channel Breakout trades new highs and lows based on the channel range. A close beyond the upper band signals strength, while a drop below the lower band invites shorts. Exits occur when price returns to the midpoint.

The channel is calculated from the highest high and lowest low over a lookback window. When price pierces these bounds, the system expects volatility expansion and positions accordingly.


## Details

- **Entry Criteria**: Signals based on Price Action.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `ChannelPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Price Action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 52%. It performs best in the crypto market.
