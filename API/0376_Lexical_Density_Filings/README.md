# Lexical Density Filings Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This factor strategy examines the language used in regulatory filings to gauge
future stock performance. Lexical density is measured as the fraction of unique
terms in the most recent report. Dense filings suggest rich, information-heavy
disclosures that often precede stronger returns, while sparse wording can mask
weakness.

Each quarter the universe is sorted by lexical density. The highest quintile is
held long and the lowest quintile is shorted, with positions equally weighted.
Rebalancing occurs during the first three trading days of February, May, August
and November and positions remain open between reviews without stop-losses.

Backtests on broad U.S. equities show the factor provides a steady premium with
moderate turnover, making it a useful component in multi-factor portfolios.

## Details

- **Entry Criteria**: Quarterly sort by lexical density; long top quintile,
  short bottom quintile
- **Long/Short**: Both
- **Exit Criteria**: Next rebalance
- **Stops**: No
- **Default Values**:
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Text analytics
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Multi-month
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
