# January Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The January Effect observes that small-cap stocks often outperform early in the year, possibly due to tax-loss selling in December.
Traders attempt to capture this tendency by buying in late December and selling after the first few weeks of January.

Testing indicates an average annual return of about 103%. It performs best in the stocks market.

The strategy follows that schedule, entering near year-end and exiting mid-January.

A stop-loss ensures losses stay manageable if the effect fails to appear.

### Executed monthly calendar rules (C# and Python)

The implemented strategy is a **monthly** adaptation, not an exclusive December/January trade. On the first eligible finished 5-minute candle of a new day 1–5, a flat strategy buys when `Close > SMA20`. On days 14–17 it closes longs; if flat and `Close < SMA20`, it may instead open a short. On days 26–31, shorts are covered. These actions require a calendar-day change and are subject to a cooldown of 50 finished candles. A native 2% stop-loss protects filled long and short positions independently of these date windows. The published return above has not been confirmed on BTC/TON history.


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

