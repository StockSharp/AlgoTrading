# Martingale Smart Strategy

## Overview

Martingale Smart is a conversion of the MetaTrader expert advisor "Martingale Smart". The strategy keeps only one open position at a time and switches between two different entry filters after every losing cycle:

1. **Primary filter** – crossover between two simple moving averages combined with the direction of a higher time frame MACD histogram. This is the default entry mode.
2. **Secondary filter** – moving average envelopes. When the floating loss of the previous cycle is negative the strategy toggles to this filter. Another loss switches back to the primary filter.

The martingale component increases the volume of the next trade after a losing cycle. You can either multiply the last volume (classic martingale) or add a fixed increment.

## Data subscriptions

* `CandleType` – timeframe used for the main calculations and trade management.
* `MacdTimeFrame` – secondary timeframe dedicated to the MACD filter. It defaults to one month in order to mimic the original EA that used the `PERIOD_MN1` timeframe.

Both subscriptions are started automatically in `OnStarted`.

## Trading logic

1. A new trade is considered only if there is no open position and all indicators are formed.
2. The primary filter goes long when the fast MA is below the slow MA and the MACD line is above the signal (same logic for bearish cases). These conditions follow the original EA that used `iMA` and `iMACD` with a one-bar shift.
3. The secondary filter uses a simple moving average envelope. A close above the lower band signals a long entry, while a close below the upper band signals a short entry. This reproduces the logic based on `iEnvelopes`.
4. When a cycle ends with a negative profit the strategy flips to the alternative filter and calculates the next volume according to the martingale parameters. A profitable cycle keeps the current filter and resets the volume to the initial value.
5. Protective stop-loss and take-profit levels are attached immediately after each entry using pip-based distances.

## Risk management

* **Break-even stop** – once the unrealized profit reaches `BreakEvenTriggerPips`, the stop-loss jumps to the entry price plus an optional offset.
* **Classic trailing stop** – maintains a moving stop that stays `TrailingStopPips` away from the latest close.
* **Take profit in money** – closes the position when the floating profit exceeds `MoneyTakeProfit`.
* **Take profit in percent** – similar to the money target but expressed as a percentage of the current portfolio value.
* **Money trailing stop** – activates when the floating profit reaches `MoneyTrailingTarget`; afterwards, the strategy keeps track of the profit peak and liquidates the position when the drawdown exceeds `MoneyTrailingDrawdown`.

All monetary calculations rely on the instrument's `PriceStep` and `StepPrice`. If the data source does not provide them, the strategy falls back to a simple price × volume estimate.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `UseMoneyTakeProfit` | Enable the fixed monetary take profit rule. |
| `MoneyTakeProfit` | Floating profit target in the account currency. |
| `UsePercentTakeProfit` | Enable the percentage-based take profit. |
| `PercentTakeProfit` | Floating profit target as % of portfolio value. |
| `EnableMoneyTrailing` | Enable profit trailing in money. |
| `MoneyTrailingTarget` | Profit level that enables the trailing block. |
| `MoneyTrailingDrawdown` | Maximum allowed profit give-back once trailing is active. |
| `UseBreakEven` | Move the stop-loss to break-even after the configured distance. |
| `BreakEvenTriggerPips` | Profit distance in pips required before the stop moves. |
| `BreakEvenOffsetPips` | Additional pips added to the break-even stop. |
| `MartingaleMultiplier` | Multiplication factor applied after a losing cycle. |
| `InitialVolume` | Volume used for the first order of every cycle. |
| `UseDoubleVolume` | If true, multiply volume; otherwise apply `LotIncrement`. |
| `LotIncrement` | Fixed lot increment used when doubling is disabled. |
| `TrailingStopPips` | Distance of the classic trailing stop in pips. |
| `StopLossPips` | Initial stop-loss distance in pips. |
| `TakeProfitPips` | Initial take-profit distance in pips. |
| `FastMaPeriod` | Period of the fast moving average. |
| `SlowMaPeriod` | Period of the slow moving average. |
| `EnvelopePeriod` | Period of the envelope moving average. |
| `EnvelopeDeviation` | Envelope width in percent. |
| `MacdFastLength` | Fast EMA length inside the MACD. |
| `MacdSlowLength` | Slow EMA length inside the MACD. |
| `MacdSignalLength` | Signal EMA length inside the MACD. |
| `CandleType` | Main signal timeframe. |
| `MacdTimeFrame` | Timeframe for the MACD candles. |

## Usage notes

1. The martingale step is executed only when the previous position was completely closed with a loss.
2. The strategy expects one open position at a time; it always liquidates the current position before entering in the opposite direction.
3. For accurate money-based thresholds, configure the instrument's contract specifications (`PriceStep`, `StepPrice`, and `VolumeStep`).
4. Break-even and trailing stops are evaluated on closed candles in the main timeframe; intrabar spikes are ignored.

## Differences vs. the MetaTrader EA

* The conversion uses StockSharp's high-level API (`SubscribeCandles` + `Bind`) and the `MovingAverageConvergenceDivergenceSignal` indicator instead of direct calls to `iMACD`.
* Some broker-specific checks (freeze levels, manual mail/notification calls, ticket-based loops) are omitted because the StockSharp engine manages those aspects internally.
* Money-based protections operate on aggregated positions rather than per-ticket calculations, aligning with StockSharp's account model.
