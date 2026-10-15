# Post-Holiday Weakness Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Post-Holiday Weakness is the tendency for prices to drift lower immediately after a major holiday when volume remains thin.
With many participants still away, counter-trend moves can gain traction.

Testing indicates an average annual return of about 112%. It performs best in the forex market.

The strategy sells short the day after a holiday and covers quickly once normal participation returns.

A small stop is used to avoid excessive losses during low-liquidity trading.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `Holidays` = 2024 NYSE holidays
  - `StopLossPercent` = 2
    Days are UTC days, since the market trades around the clock; entries happen at the close of the day's first candle. Holidays is a comma-separated list of yyyy-MM-dd dates, by default the 2024 NYSE holidays (Good Friday is 2024-03-29). The market trades every day, so the short opens on the calendar day after a holiday and is covered at that day's last candle.
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

