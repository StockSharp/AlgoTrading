# RSI Eraser Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RSI Eraser strategy is a direct port of the MetaTrader 5 expert advisor created by Vladimir Karputov.
It uses hourly candles to evaluate the relative strength index (RSI) and looks for mean-reversion style entries when momentum shifts around the neutral 50 level.
Trades are filtered by the previous daily high/low range and the strategy sizes each position according to a fixed percentage of account equity.

## Core Logic

- **Primary timeframe** – 1 hour candles drive the indicator calculations and trade signals.
- **Filter timeframe** – Completed daily candles provide yesterday's high and low that gate entries.
- **Indicator** – Classic RSI with configurable lookback length.
- **Direction** – Long when RSI > neutral level, short when RSI < neutral level.
- **Risk sizing** – Position volume is derived from the distance between entry and stop multiplied by the chosen risk percentage.

## Entry Rules

1. Wait for the hourly candle to close and compute RSI.
2. Check that at least one completed daily candle is available.
3. **Long setup**
   - RSI value strictly above the neutral threshold (default 50).
   - Proposed stop level (entry − stop loss distance) must not be below yesterday's low minus the daily buffer.
   - Entry is rejected if a long trade has already been opened on the same calendar day.
4. **Short setup**
   - RSI value strictly below the neutral threshold.
   - Proposed stop level (entry + stop loss distance) must not be above yesterday's high plus the daily buffer.
   - Entry is rejected if a short trade has already been opened on the same calendar day.
5. When conditions are satisfied the strategy sends a market order with risk-based volume.
   If there is an opposite position, the new order closes it and flips direction in a single operation.

## Exit Rules

- Initial stop-loss and take-profit are computed from the configured pip distance and multiplier.
- The strategy continually monitors completed candles:
  - A long trade exits when price trades down to the stop or up to the take-profit level.
  - A short trade exits when price trades up to the stop or down to the take-profit level.
- Break-even protection: once price moves in favor by at least the original stop distance,
  the stop is raised (or lowered for shorts) to the exact entry price.
- When no position is open all risk levels are cleared to avoid stale values.

## Risk Management

- `RiskPercent` defines the fraction of portfolio equity to risk on each trade.
- Position size is calculated as `risk_amount / stop_distance` with a fallback to the base strategy `Volume` when equity information is unavailable.
- The daily buffer adds an extra safety margin around yesterday's range, preventing trades that would place stops too close to recent swing extremes.

## Default Parameters

- `RsiPeriod` = 14
- `RsiNeutralLevel` = 50
- `StopLossPips` = 50
- `TakeProfitMultiplier` = 3
- `DailyBufferPips` = 10
- `RiskPercent` = 5%
- `CandleType` = 1 hour
- `DailyCandleType` = 1 day

## Implementation Notes

- The strategy subscribes to both hourly and daily candle feeds using the high-level StockSharp API.
- All comments and log messages are provided in English to match repository guidelines.
- Break-even handling and the single-trade-per-day restriction follow the original MetaTrader logic.
