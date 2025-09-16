# Backbone Strategy

This strategy reproduces the core behaviour of the original **Backbone** MQL5 expert advisor using the StockSharp high-level API. It alternates between bullish and bearish trading cycles, scales into positions according to a risk fraction, and protects open trades with fixed targets together with a trailing stop.

## Core Idea

1. **Initial direction detection** – the strategy tracks the highest high and the lowest low after startup. A move larger than the trailing-stop distance away from either extreme defines which side will trade first.
2. **Directional cycles** – after a cycle starts, the algorithm only trades in that direction until all positions are closed. When the last position exits, it immediately flips and prepares for the opposite cycle.
3. **Risk-based scaling** – each additional entry uses a dynamic volume derived from the account equity, the `MaxRisk` fraction, the configured limit `MaxTrades`, and the stop-loss distance. This mimics the lot-sizing function from the original EA.
4. **Protective exits** – every entry recalculates a stop-loss and a take-profit level around the volume-weighted average price of the current cycle. A trailing stop tightens the protective stop whenever the unrealised profit exceeds the configured trailing distance.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaxRisk` | 0.5 | Fraction of account equity available for all positions in the current direction. |
| `MaxTrades` | 10 | Maximum number of sequential entries per directional cycle. |
| `TakeProfitPips` | 170 | Distance (in pips) between the entry average and the take-profit target. |
| `StopLossPips` | 40 | Distance (in pips) between the entry average and the protective stop. |
| `TrailingStopPips` | 300 | Distance (in pips) used both to determine the initial direction and to trail profits. |
| `CandleType` | 5-minute time frame | Candle type used for signal evaluation. |

> **Pip definition** – the strategy automatically adjusts the pip size based on the instrument `PriceStep`. Symbols quoted with 3 or 5 decimal places use a 10× multiplier, which replicates the original MetaTrader pip handling.

## Trading Logic

1. Wait for a finished candle. Skip processing while the strategy is warming up or trading is disabled.
2. Update the extreme prices while no direction has been chosen yet. Once the high breaks upward (by more than `TrailingStopPips`) the first cycle will be short; if the low breaks downward, the first cycle will be long.
3. While the cycle is long:
   - Add a new long entry when either (a) the previous cycle was short and no long positions are open, or (b) the previous cycle was also long and the number of open longs is below `MaxTrades`.
   - Exit the entire long cycle when the take-profit or stop-loss is reached, or when the trailing stop raises the protective level above the current stop.
4. While the cycle is short the same rules apply with inverted conditions.
5. After a cycle closes, reset its counters and wait for the opposite setup.

## Position Sizing

The position size for each new entry is calculated as:

```
qty = equity * fraction / (pipSize * stopLoss)
where fraction = 1 / (MaxTrades / MaxRisk - openTrades)
```

The quantity is then aligned to the instrument volume step and capped within the minimum/maximum volume bounds. If the required size falls below the allowed minimum, the minimum is used. When equity information is unavailable, the default strategy volume acts as a fallback.

## Exit Management

- **Stop-loss / take-profit** – recalculated whenever a new order is added so that all trades in the current cycle share the same combined levels based on the average entry price.
- **Trailing stop** – for a long cycle the stop moves to `Close - TrailingStopPips * pipSize` once the unrealised profit exceeds that threshold. The short-side trailing mirror is handled symmetrically.

## Notes and Limitations

- StockSharp executes trades in a netting environment, therefore each directional cycle manages the combined position instead of individual tickets. The alternating logic and risk formula reproduce the original behaviour while fitting the API model.
- The strategy relies on completed candles. Intrabar movements smaller than the candle range are not evaluated.
- Ensure that the selected candle type and security produce enough data to build the initial extremes before expecting trades.

