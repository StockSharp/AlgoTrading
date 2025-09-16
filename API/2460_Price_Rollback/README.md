# Price Rollback Strategy

This strategy trades daily price gaps.
At the beginning of a selected weekday it compares the last closing price with the opening price 24 hours earlier.
If the gap is greater than the **Corridor** parameter it opens a position in the direction of rollback:

- Gap up → sell.
- Gap down → buy.

Trades use fixed stop-loss and take-profit in price units.
A trailing stop with step is applied after the position moves in profit.
All positions are closed near the end of the day (22:45).

## Parameters
- `Corridor` – gap threshold.
- `StopLoss` – fixed loss distance.
- `TakeProfit` – fixed profit target.
- `TrailingStop` – trailing distance.
- `TrailingStep` – movement required to update trailing.
- `TradingDay` – weekday for opening trades (0=Sunday).
- `CandleType` – time frame for calculations.
