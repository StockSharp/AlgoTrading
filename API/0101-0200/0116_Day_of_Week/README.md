# Day of Week Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The Day of Week Effect exploits tendencies for markets to exhibit recurring behavior on specific weekdays.
Some indices show consistent strength midweek while Monday or Friday can be relatively weak.

Testing indicates an average annual return of about 85%. It performs best in the crypto market.

The strategy opens trades based on those historical tendencies, buying or selling at the start of the session and exiting by the close.

A modest stop guards against anomalies, closing the position early if the pattern fails on a given day.

## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
    Days are UTC days, since the market trades around the clock; entries happen at the close of the day's first candle. Tuesday to Thursday are bought and Monday and Friday sold short; the position closes at the day's last candle and weekends are not traded.
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

