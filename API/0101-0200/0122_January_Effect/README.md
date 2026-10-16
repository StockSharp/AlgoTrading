# January Effect Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This example trades **recurring windows within every calendar month** and uses a moving-average filter. It is **not** confined to the classical late-December/early-January small-cap January effect and does not use company-size or tax-loss data. The previously stated 103% annual return is not demonstrated by the BTC/TON fixture.

### Executed monthly calendar rules (C# and Python)

The implemented strategy is a **monthly** adaptation, not an exclusive December/January trade. On the first eligible finished 5-minute candle of a new day 1–5, a flat strategy buys when `Close > SMA20`. On days 14–17 it closes longs; if flat and `Close < SMA20`, it may instead open a short. On days 26–31, shorts are covered. These actions require a calendar-day change and are subject to a cooldown of 50 finished candles. A native 2% stop-loss protects filled long and short positions independently of these date windows. The published return above has not been confirmed on BTC/TON history.


## Details

- **Entry Criteria**: calendar effect triggers
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
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

