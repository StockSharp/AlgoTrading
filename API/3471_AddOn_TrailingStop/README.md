# AddOn Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Port of the MetaTrader expert **AddOn_TrailingStop**. The strategy does not open positions on its own and only adjusts trailing stops for an existing net position.

## How it works

- Subscribes to Level1 data to monitor the latest best bid and ask quotes.
- Calculates the pip size from the security decimals so the inputs behave like in MetaTrader (4/5 digits = 0.0001 pip, 2/3 digits = 0.01 pip).
- When a long position is open and the bid price advances by `TrailingStartPips` pips, the strategy moves the internal trailing stop to `Bid - TrailingStartPips` pips.
- The long stop is only advanced when the new level is at least `TrailingStepPips` pips higher than the previous stop.
- When a short position is open and the ask price drops by `TrailingStartPips` pips, the strategy moves the internal trailing stop to `Ask + TrailingStartPips` pips.
- The short stop is only advanced when the new level is at least `TrailingStepPips` pips lower than the previous stop.
- If the current quote crosses the trailing stop, the strategy closes the entire position at market and resets its state.

## Parameters

- `EnableTrailing` (default **true**) – enables or disables trailing stop management.
- `TrailingStartPips` (default **15**) – profit in pips required before trailing activates.
- `TrailingStepPips` (default **5**) – extra profit in pips required before the stop can move again.
- `MagicNumber` (default **0**) – identifier kept for parity with the MQL expert. It is informational because StockSharp operates on the current strategy position.

## Notes

- Requires a configured `Security`, `Portfolio`, and Level1 data feed.
- Designed to complement other strategies that handle entries.
- Uses `StrategyParam<T>` so every input can be optimized or exposed in the UI.
- Sends `BuyMarket`/`SellMarket` orders when the trailing stop is hit because StockSharp automatically manages protective orders after position exits.
