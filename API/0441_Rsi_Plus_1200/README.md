# RSI + 1200 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **RSI + 1200 Strategy** seeks to capture trend reversals confirmed by
relative strength and a higher time frame trend filter. It combines a classic
14‑period Relative Strength Index with an Exponential Moving Average calculated
on a 120‑minute multi‑time frame series ("1200" refers to the higher time frame
in the original concept). Trading signals are only taken when momentum and the
trend filter align.

Backtests on liquid cryptocurrency pairs show that the method performs best in
sustained directional markets. Choppy or range‑bound periods can produce false
signals, so the strategy includes a small price slack around the EMA and a
percentage based stop‑loss to help manage risk.

A long trade is opened when the RSI crosses upward from oversold territory and
price is within one percent above the higher‑time‑frame EMA. The short setup is
the mirrored condition. Positions are closed when the RSI reaches the opposite
extreme, signalling exhaustion of the move. A protective stop is also placed at
`stopLossPercent` percent from the entry price.

## Details

- **Entry Conditions**
  - **Long**: RSI crosses above `rsiOversold` and close is <= 1% above EMA.
  - **Short**: RSI crosses below `rsiOverbought` and close is >= 1% below EMA.
- **Exit Conditions**
  - **Long**: RSI rises above `rsiOverbought`.
  - **Short**: RSI falls below `rsiOversold`.
- **Stops**: Optional percentage stop‑loss via `stopLossPercent`.
- **Default Parameters**
  - `rsiLength` = 14
  - `rsiOverbought` = 72
  - `rsiOversold` = 28
  - `emaLength` = 150
  - `mtfTimeframe` = 120 minutes
  - `stopLossPercent` = 0.10 (10%)
- **Filters**
  - Category: Trend following
  - Direction: Both
  - Indicators: RSI, EMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday / multi‑time frame
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate
