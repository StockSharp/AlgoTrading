# Avalanche AV Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Avalanche AV is a randomized martingale strategy that alternates between long and short entries with equal probability. Trades are opened only after a configurable number of finished candles, and every position inherits fixed stop-loss and take-profit levels defined in pips. When a trade closes at a loss the position size is multiplied by the martingale coefficient to chase recovery; profitable trades reset the size back to the starting volume once the account balance prints a new equity high. The strategy also enforces a maximum floating drawdown as a percentage of account balance and will close any position that breaches this threshold.

The original MQL version opened trades on ticks. The StockSharp port keeps the same probabilistic behaviour but works on candle updates, making it suitable for both backtesting and live trading with bar data.

## Trading Rules

- **Decision interval:** wait for the specified number of finished candles before evaluating a new signal. If a position is still open the interval continues counting but no new trade is taken.
- **Entry direction:** generate a random number; values above 16384 trigger a long entry, otherwise a short entry. Positions are opened only when there is no active trade.
- **Order size:** start with `InitialVolume`. After each losing trade the next order size becomes `PreviousVolume * MartingaleMultiplier` (normalized to the instrument's volume step). Winning trades reset the size to `InitialVolume` once the realised balance makes a new peak; otherwise the martingale expansion continues.
- **Stops and targets:** stop-loss and take-profit are calculated in pips from the entry price. A pip equals the instrument's price step.
- **Floating drawdown:** while a position is active, the strategy monitors unrealised PnL. If the loss exceeds `MaxDrawdownPercent` of the realised account balance (`initial balance + realised PnL`), the position is closed immediately.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `InitialVolume` | 0.1 | Starting trade volume. |
| `StopLossPips` | 15 | Stop distance in pips (0 disables the stop). |
| `TakeProfitPips` | 30 | Take profit distance in pips (0 disables the target). |
| `MaxDrawdownPercent` | 75 | Maximum tolerated floating loss as percent of balance. |
| `MartingaleMultiplier` | 1.6 | Volume multiplier applied after a loss. |
| `DecisionInterval` | 9 | Number of finished candles between new trade decisions. |
| `CandleType` | 1-minute time frame | Candle type driving the strategy. |

## Notes

- Volume is automatically normalized to the instrument's `VolumeStep`, `MinVolume`, and `MaxVolume` limits. If normalization fails the size resets to the initial volume.
- Stop-loss and take-profit levels rely on the instrument's `PriceStep` as one pip; verify the step for exotic symbols.
- The drawdown protection requires both `PriceStep` and `StepPrice` to be defined; otherwise the safety check is skipped.
- Because the strategy relies on randomness, results vary between runs even with identical market data unless the random seed is controlled externally.
