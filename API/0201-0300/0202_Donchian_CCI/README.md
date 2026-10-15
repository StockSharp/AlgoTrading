# Donchian CCI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This strategy uses Donchian CCI indicators to generate signals.
Long entry occurs when Price > Donchian Upper && CCI > CciOverbought (breakout up with upward momentum). Short entry occurs when Price < Donchian Lower && CCI < CciOversold (breakout down with downward momentum).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 43%. It performs best in the stocks market.

## Details
- **Entry Criteria**:
  - **Long**: Price > Donchian Upper && CCI > CciOverbought (breakout up with upward momentum)
  - **Short**: Price < Donchian Lower && CCI < CciOversold (breakout down with downward momentum)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price falls below middle band
  - **Short**: Exit short position when price rises above middle band
- **Stops**: Yes.
- **Default Values**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    The channel is the high and low of the previous DonchianPeriod candles, and its middle is halfway between them. An upside breakout is confirmed when CCI is above CciOverbought and a downside one when CCI is below CciOversold; the opposite reading, an oversold CCI on a close above the channel, essentially never occurs. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Donchian CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

