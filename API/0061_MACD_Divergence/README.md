# MACD Divergence

MACD Divergence looks for disagreement between price action and the MACD indicator. Higher highs in price but lower highs in MACD suggest weakening momentum (bearish divergence), while lower lows in price and higher MACD lows hint at bullish reversal.

After detecting divergence, the system waits for MACD to cross its signal line before entering. The trade is closed if MACD crosses back or the stop-loss triggers.

## Rules

- **Entry Criteria**: Bullish or bearish divergence plus MACD crossing signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: MACD crosses opposite direction or stop.
- **Stops**: Yes.
- **Default Values**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `DivergencePeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 2.0m
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium
