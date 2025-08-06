# F-Score Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy blends Piotroski F-Score fundamentals with short-term price reversal. Each month it buys the worst-performing stock among those with strong F-Scores and optionally shorts the best performer with weak F-Scores. The premise is that fundamentally sound firms snap back after temporary declines while weak firms revert after rallies.

On the first trading day of the month the algorithm ranks the universe by one-month return. It goes long the lowest-returning security with `FScore >= FHi` and, if available, shorts the highest-returning security with `FScore <= FLo`. Positions are held for one month.

## Details

- **Entry Criteria**:
  - Long: among securities with `FScore >= FHi`, buy the one with the lowest `Lookback` return if trade size >= `MinTradeUsd`.
  - Short (optional): among securities with `FScore <= FLo`, short the one with the highest `Lookback` return.
- **Long/Short**: Long and short.
- **Exit Criteria**: Close all positions at the next monthly rebalance.
- **Stops**: None.
- **Default Values**:
  - `Universe` – securities to evaluate.
  - `Lookback` = 21 days.
  - `FHi` = 7.
  - `FLo` = 3.
  - `CandleType` = 1 day.
  - `MinTradeUsd` – minimum trade value.
- **Filters**:
  - Category: Mean reversion.
  - Direction: Long & short.
  - Timeframe: Short-term.
  - Rebalance: Monthly.

