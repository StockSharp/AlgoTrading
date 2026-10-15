# Santa Claus Rally Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The Santa Claus Rally describes the tendency for stocks to rise in the final week of December through the first two trading days of January.
Holiday optimism and year-end positioning can fuel this short burst of strength.

Testing indicates an average annual return of about 100%. It performs best in the forex market.

The strategy buys at the start of the period and exits after the second trading day of the new year, aiming to capture the seasonal lift.

Stops are kept small to avoid large losses if the market fails to rally during the window.

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

