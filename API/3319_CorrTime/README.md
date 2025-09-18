# CorrTime Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The CorrTime strategy is a single-symbol system that replicates the MetaTrader expert advisor of the same name. It analyses the correlation between closing prices and their chronological order to detect acceleration or reversal of momentum. The algorithm operates on completed candles and combines three layers of confirmation:

1. **Volatility filter** – the Bollinger Band width must sit inside a configurable band of acceptable activity, so the system avoids flat and excessively volatile phases.
2. **Trend strength filter** – the Average Directional Index (ADX) must remain above a threshold before correlation signals are evaluated.
3. **Correlation triggers** – Pearson, Spearman, Kendall or Fechner correlation estimators measure how closely price evolves with time. A sudden change of the coefficient generates the trading decision.

Although the original robot was designed for EURUSD on the H1 timeframe, the StockSharp version keeps all parameters configurable. The default settings remain faithful to the source (1-hour candles, Fechner correlation, reverse trading mode).

## Trading Workflow

1. Subscribe to the selected `CandleType` and wait for a finished bar.
2. Update the Bollinger Bands and ADX values on the new candle.
3. Reject the bar when:
   - The Bollinger spread converted to pips is outside `[BollingerSpreadMin, BollingerSpreadMax]`.
   - ADX is below `AdxLevel`.
   - The candle begins outside the `[EntryHour, EntryHour + OpenHours]` trading window (with wrap-around support).
4. Build a rolling history of closing prices and calculate the correlation coefficient over `CorrelationRangeTrend` and `CorrelationRangeReverse` lookbacks. The code re-computes the latest three correlation values in order to detect an actual crossing of the limits, exactly like the original include file did with buffers.
5. Trend-following trigger (when `TradeMode` is *TrendFollow* or *Both*):
   - **Long** – correlation was below `CorrLimitTrendBuy`, is still below on the previous bar, and crosses above the threshold on the latest bar.
   - **Short** – correlation was above `-CorrLimitTrendSell`, is still above on the previous bar, and crosses below `-CorrLimitTrendSell` on the latest bar.
6. Reversal trigger (when `TradeMode` is *Reverse* or *Both*):
   - **Long** – correlation was below `-CorrLimitReverseBuy`, is still below on the previous bar, and climbs above `-CorrLimitReverseBuy` on the latest bar.
   - **Short** – correlation was above `CorrLimitReverseSell`, is still above on the previous bar, and drops below `CorrLimitReverseSell` on the latest bar.
7. If both directions fire simultaneously the signals cancel each other, mirroring the MetaTrader behaviour.
8. If `CloseTradeOnOppositeSignal` is enabled, the strategy immediately closes any opposite position before opening a new one.
9. Entries are sized with the `Volume` property and respect `MaxOpenOrders`, so the net exposure never exceeds `Volume * MaxOpenOrders` in either direction.
10. Risk is controlled through `StartProtection`: stop-loss and take-profit use pip-based distances, and the trailing flag reuses the same stop distance when enabled.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe used to generate candles and feed all indicators. |
| `CloseTradeOnOppositeSignal` | Close open positions when the next signal points in the opposite direction. |
| `EntryHour`, `OpenHours` | Defines the daily trading window. `OpenHours = 0` keeps the window open for a single hour. |
| `BollingerPeriod`, `BollingerDeviation` | Standard Bollinger Bands settings applied to close prices. |
| `BollingerSpreadMin`, `BollingerSpreadMax` | Minimum and maximum width (in pips) required for the Bollinger channel. |
| `AdxPeriod`, `AdxLevel` | Average Directional Index configuration and the minimal trend strength required. |
| `TradeMode` | Choose between trend-following, reversal or a combined evaluation. |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | Lookback lengths for the correlation calculations. |
| `CorrelationType` | Selects Pearson, Spearman, Kendall or Fechner correlation formulas. |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | Thresholds that define a valid trend-following breakout. |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | Thresholds that define a valid reversal breakout. |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | Risk parameters expressed in pips and translated to price units with the instrument pip size. |
| `MaxOpenOrders` | Upper bound on the aggregated number of entries (per-side cap equal to `Volume * MaxOpenOrders`). |

## Practical Notes

- Pip size is deduced from the security decimals (5 or 3 decimal places correspond to a 10× multiplier) to mimic the MetaTrader point handling. Adjust the thresholds when working with non-forex assets.
- The correlation buffers need at least `lookback + 2` completed candles to evaluate a crossing. During the warm-up phase the strategy remains idle.
- Because all logic is executed on finished candles, the strategy is resilient to intrabar noise and mirrors the original behaviour that relied on `iTime` and `iClose` snapshots.
- Combine this strategy with portfolio-level risk controls when deploying multiple instances, since the original robot also limited the total number of orders across symbols.
