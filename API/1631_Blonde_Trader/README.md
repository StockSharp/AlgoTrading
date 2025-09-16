# Blonde Trader Strategy

Blonde Trader is a grid-based trading strategy converted from MQL. It searches for price moving away from recent extremes and opens positions with a grid of pending orders.

## Concept

- Calculate the highest high and lowest low over the last **Period X** candles.
- If the current price is below the recent high by more than **Limit** ticks, open a long position and place a series of buy limit orders forming a grid.
- If the current price is above the recent low by more than **Limit** ticks, open a short position and place a series of sell limit orders forming a grid.
- Close all positions when the accumulated profit reaches **Amount**.
- Optionally, after the price moves **LockDown** ticks in profit, a stop order is placed at the breakeven level to protect the position.

## Parameters

| Name | Description |
| ---- | ----------- |
| `PeriodX` | Lookback period for highest high and lowest low. |
| `Limit` | Minimal distance in ticks from current price to an extreme. |
| `Grid` | Step in ticks between grid pending orders. |
| `Amount` | Profit target in account currency. |
| `LockDown` | Distance in ticks to move stop to breakeven. |
| `CandleType` | Type of candles used for analysis. |

## Indicators

- `Highest` – tracks the highest high over the lookback period.
- `Lowest` – tracks the lowest low over the lookback period.

## Order Logic

1. When a long setup appears:
   - Buy at market with the default strategy volume.
   - Place four additional buy limit orders below the entry, each separated by **Grid** ticks and doubling the volume.
2. When a short setup appears:
   - Sell at market with the default strategy volume.
   - Place four additional sell limit orders above the entry with the same grid and volume doubling rules.
3. If `PnL` reaches **Amount**, all open positions and pending orders are closed.
4. If `LockDown` is greater than zero and the price has moved the specified number of ticks in favor of the position, a protective stop order is placed one tick beyond the entry price.

## Notes

This strategy demonstrates basic grid trading logic. It uses only high-level API features: `SubscribeCandles`, indicator binding and simple order helpers such as `BuyMarket`, `SellLimit` and `SellStop`.
