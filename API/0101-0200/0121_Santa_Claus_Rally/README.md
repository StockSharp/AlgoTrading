# Santa Claus Rally Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This implementation is a **calendar-day-of-month trading example**, using the same monthly windows throughout the year. It is not an implementation limited to the traditional final December trading days and first January trading days. The former 100% annual-return claim is not validated by the shipped BTC/TON replay.

### Executed monthly calendar rules (C# and Python)

This example **generalizes** the Santa Claus pattern to **every month**; it does not trade December/January exclusively. On the first eligible finished 5-minute candle of a day 25–31, it buys if flat and covers an existing short. On days 3–7, it sells a held long. On days 12–18, it may open a short when flat and `Close < SMA20`. Actions require a new calendar day and are subject to an entry/exit cooldown of 50 finished candles. Native 2% stop-loss protection also closes filled positions when reached, independently of the calendar. The claimed annual return above is not verified by the BTC/TON fixture.


## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2%
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

