# Hercules Strategy

The Hercules strategy is a StockSharp port of the MetaTrader expert **Hercules v1.3 (Majors)**. It combines a fast/slow moving average crossover with multi-timeframe confirmation filters and executes two independent profit targets per signal.

## Trading Logic

* **Signal arm** – compute a fast EMA (default 1 period) on candle closes and a slow SMA (72 periods) on candle opens. Detect crossovers that happened on the last or the penultimate bar. The crossover price is averaged across both moving averages, and a trigger level is placed `TriggerPips` above (for longs) or below (for shorts).
* **Execution window** – once a crossover is detected, the setup remains valid for two full bars. Only when the current close exceeds the trigger price inside this window is the order allowed to fire.
* **Filters** –
  * H1 RSI (default length 10, typical price input) must be above `RsiUpper` for longs and below `RsiLower` for shorts.
  * The current close must break the recent high/low collected over `LookbackMinutes` of candles on the trading timeframe.
  * Daily envelope (SMA 24 with ±`DailyEnvelopeDeviation`%) requires the price to close outside the band in the direction of the trade.
  * H4 envelope (SMA 96 with ±`H4EnvelopeDeviation`%) adds a second higher-timeframe confirmation.
* **Risk management** – the stop-loss is set to the high/low of the bar four candles back. Volume can be fixed (`OrderVolume`) or recalculated from `RiskPercent` of the current portfolio value.
* **Trade management** – each signal opens two market orders of equal volume. The first is liquidated at `TakeProfitFirstPips`, the second at `TakeProfitSecondPips`. A trailing stop of `TrailingStopPips` keeps both orders protected. When either the stop or both targets complete, the strategy enters a blackout period of `BlackoutHours` during which no new trades are taken.

## Parameters

| Parameter | Description |
| --- | --- |
| `OrderVolume` | Volume of each market order before money-management adjustments. |
| `UseMoneyManagement` | When enabled, recomputes the volume from `RiskPercent` of the portfolio and the current stop distance. |
| `RiskPercent` | Percentage of the portfolio value to risk per setup. |
| `TriggerPips` | Distance from the crossover price that must be exceeded to allow an entry. |
| `TrailingStopPips` | Trailing stop distance in pips applied to the combined position. |
| `TakeProfitFirstPips` | Pip distance of the first partial take profit. |
| `TakeProfitSecondPips` | Pip distance of the second partial take profit. |
| `FastPeriod` | Length of the fast EMA trigger line. |
| `SlowPeriod` | Length of the slow SMA baseline. |
| `RsiPeriod` | Length of the RSI confirmation filter. |
| `RsiUpper` / `RsiLower` | RSI thresholds that enable long and short trades. |
| `LookbackMinutes` | Window (in minutes) used to compute the recent high/low breakout filter. |
| `BlackoutHours` | Hours to pause after an execution before accepting a new setup. |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | Parameters of the daily envelope filter. |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | Parameters of the H4 envelope filter. |
| `CandleType` | Main timeframe used for trade execution. |
| `RsiTimeFrame` | Timeframe that feeds the RSI indicator. |
| `DailyTimeFrame` | Timeframe that feeds the daily envelope calculation. |
| `H4TimeFrame` | Timeframe that feeds the H4 envelope calculation. |

## Files

* `CS/HerculesStrategy.cs` – C# implementation of the Hercules strategy.
* `README.md` – this document.
* `README_ru.md` – Russian description.
* `README_cn.md` – Chinese description.
