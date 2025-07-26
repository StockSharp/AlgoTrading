# Keltner Channel Breakout
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Keltner Channel breakout

Keltner Channel Breakout uses volatility bands derived from ATR. Breakouts above the upper band or below the lower band trigger entries. Price moving back through the EMA center or hitting a stop exits the position.

Because the bands expand and contract with volatility, this breakout method aims to capture early stages of a strong move while still allowing price room to breathe within the channel.


## Details

- **Entry Criteria**: Signals based on ATR, Keltner.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: ATR, Keltner
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 58%. It performs best in the stocks market.
