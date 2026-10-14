# Pre-Holiday Strength Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Pre-Holiday Strength refers to the bullish tendency just before major market holidays when volume is lighter and sentiment optimistic.
Traders often position ahead of the break, pushing prices higher in the final session or two.

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

The strategy goes long on the day before a holiday and exits the following session or at the close, capturing that short-term bias.

A tight stop is used in case the expected lift doesn't occur.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `Holidays` = 2024 NYSE holidays
  - `StopLossPercent` = 2
    Days are UTC days, since the market trades around the clock; entries happen at the close of the day's first candle. Holidays is a comma-separated list of yyyy-MM-dd dates, by default the 2024 NYSE holidays (Good Friday is 2024-03-29). The market trades every day, so the long opens on the calendar day before a holiday and closes at that day's last candle.
- **Filters**:
  - Category: Seasonality
  - Direction: Both
  - Indicators: Seasonality
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

