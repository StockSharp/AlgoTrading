# EMA (barabashkakvn Edition) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Converted from the MetaTrader 5 expert advisor "EMA (barabashkakvn's edition)". The system trades the crossover of two exponential moving averages that are calculated on the median price and uses virtual take-profit/stop-loss levels expressed in pips. Positions are opened only after a confirmed crossover and a small retracement toward the previous candle extreme.

## Core Idea

1. Track 5- and 10-period EMAs (median price) on the selected timeframe.
2. When the fast EMA crosses the slow EMA, arm a pending signal instead of trading immediately.
3. Wait for price to retrace `MoveBackPips` from the previous candle extremum while the EMA spread exceeds `2 * pipSize`.
4. Enter in the direction of the crossover once the retracement occurs.
5. Manage the open position with virtual targets and stops measured in pips from the entry price.

This behaviour mirrors the original MQL implementation: the expert waited for the crossover flag (`check`) and then required an EMA spread plus a price retracement relative to the previous candle to trigger the trade. The exit rules also follow the "virtual" approach by closing positions when the bid/ask would have touched the specified distances.

## Indicators & Data

- 5-period EMA on median price (high + low) / 2.
- 10-period EMA on median price.
- Previous finished candle high/low for retracement checks.
- All processing uses finished candles from the configured `CandleType` subscription.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Trading volume in lots/contracts for each entry. |
| `VirtualProfitPips` | `5` | Distance (in pips) between entry price and virtual take-profit. |
| `MoveBackPips` | `3` | Retracement required after the crossover, measured from the previous candle extremum. |
| `StopLossPips` | `20` | Distance (in pips) between entry price and virtual stop-loss. |
| `PipSize` | `0.0001` | Pip size expressed in price units. Override when trading symbols with a different pip definition. |
| `FastLength` | `5` | Length of the fast EMA. |
| `SlowLength` | `10` | Length of the slow EMA. |
| `CandleType` | `TimeFrame(1m)` | Candle source used for calculations. |

All pip-based values are converted to price distances using `pipValue = PipSize`. If the parameter is left at zero or a negative number the strategy falls back to `Security.PriceStep` (when provided by the board).

## Trading Logic

### Entry Conditions

- **Signal arming**: store a pending signal whenever a crossover occurs (`FastEMA` crosses above `SlowEMA` or vice versa). No trade is placed yet.
- **Short entry**: requires
  - Pending signal present.
  - `SlowEMA - FastEMA > 2 * pipSize`.
  - Current candle high ≥ previous candle low + `MoveBackPips * pipSize` (price retraced upward from the prior low).
- **Long entry**: requires
  - Pending signal present.
  - `FastEMA - SlowEMA > 2 * pipSize`.
  - Current candle low ≤ previous candle high - `MoveBackPips * pipSize` (price retraced downward from the prior high).

After opening a position the pending flag resets to avoid duplicate entries.

### Exit Conditions

Virtual targets emulate the MQL behaviour by comparing the candle extremes with the preset distances:

- **Long position**:
  - Close if candle high ≥ entry price + `VirtualProfitPips * pipSize`.
  - Close if candle low ≤ entry price - `StopLossPips * pipSize`.
- **Short position**:
  - Close if candle low ≤ entry price - `VirtualProfitPips * pipSize`.
  - Close if candle high ≥ entry price + `StopLossPips * pipSize`.

After any exit the virtual levels reset and the strategy waits for the next crossover.

## Implementation Notes

- Uses the high-level candle subscription (`SubscribeCandles`) and draws EMAs plus trades on the optional chart area.
- Median price is computed directly from the candle high/low to match `PRICE_MEDIAN` from MetaTrader.
- The crossover flag (`_hasCrossSignal`) reproduces the original `check` variable, ensuring trades only occur after both crossover and retracement checks.
- `StartProtection()` is called in `OnStarted` to enable built-in risk monitoring even though the strategy handles exits manually.
- The code keeps all comments in English, as requested, and relies solely on finished candles without accessing indicator buffers directly.

## Usage Tips

- Adjust `PipSize` when running on instruments with non-standard pip definitions (e.g., JPY pairs, indices, crypto quotes).
- Because exits rely on candle extremes, using shorter timeframes (1–5 minutes) keeps behaviour closer to the original tick-based expert.
- Optimization can explore EMA lengths, pip distances, and retracement values using the provided parameter metadata.
- The strategy trades one position at a time; any external positions on the same security can interfere with the virtual exit tracking.

## Risks

- Candle-based simulation may miss intrabar touches of the virtual levels; consider higher-resolution data if precision is critical.
- Virtual exits do not place real protective orders, so disconnections or slippage can lead to larger losses than expected in live trading.
- As with any crossover system, performance degrades in ranging markets; combine with filters if necessary.
