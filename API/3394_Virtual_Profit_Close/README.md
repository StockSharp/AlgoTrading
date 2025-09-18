# Virtual Profit Close Strategy

## Overview

Virtual Profit Close replicates the behaviour of the MetaTrader 4 expert advisor *Virtual_Profit_Close.mq4*. The strategy watches the
current position of the configured security and exits as soon as a virtual profit target is reached. Unlike a regular take-profit order,
the exit level is evaluated internally so no profit orders are left in the order book. A configurable trailing stop can move the exit
price closer to market as the trade moves into profit. When running in testing mode the strategy can automatically open sample positions
to demonstrate its logic.

## Conversion Notes

- Tick events are consumed through `SubscribeTrades().Bind(ProcessTrade).Start()` to mimic the original `OnTick` routine.
- MetaTrader "points" are converted to pips by inspecting `Security.PriceStep` and adjusting for 3/5 digit symbols.
- Virtual profit and trailing calculations use the current bid for long positions and the ask for short positions, matching the MQL
  implementation that relied on `Bid` and `Ask` prices.
- The trailing stop logic activates after the configured profit threshold and keeps the stop at a fixed distance from the market
  price, similar to repeatedly calling `OrderModify` in MQL.
- A demonstration mode replaces the original strategy tester helper (`SendTest`) by opening market orders according to the selected
  direction and volume. Optional protective stops are placed using `SetStopLoss`.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `ProfitPips` | Virtual take-profit level expressed in MetaTrader pips. The strategy closes the position once the profit exceeds this distance. |
| `UseTrailingStop` | Enables trailing behaviour when set to `true`. |
| `TrailingOffsetPips` | Distance maintained between the current price and the trailing stop once it is active. |
| `TrailingActivationPips` | Minimum profit in pips required before the trailing stop is engaged. |
| `EnableDemoMode` | Automatically opens demonstration orders each time the position becomes flat. Useful for backtests. |
| `DemoOrderDirection` | Direction of demo orders (`Buy` or `Sell`). |
| `DemoOrderVolume` | Volume submitted for demo orders. |
| `DemoStopPips` | Optional protective stop for demo orders, expressed in pips. |

## Behaviour

1. When the strategy starts it calculates the pip size and distances for profit, trailing and demo stops.
2. Every tick received through `ProcessTrade` evaluates the current position:
   - Long positions are closed when the bid price delivers the configured virtual profit.
   - Short positions are closed when the ask price covers the same distance in the opposite direction.
3. If trailing is enabled and the activation threshold is met, the trailing stop moves together with the favourable price movement. Once
   the market crosses the trailing level the strategy sends a market order to exit.
4. Demo mode can automatically open a new position whenever the strategy becomes flat, recreating the tester-only feature of the
   original expert.

## Requirements

- The strategy needs tick-level market data to respond precisely to price changes.
- Only one symbol should be assigned to the strategy instance. Multiple simultaneous symbols are not supported, matching the original
  MQL implementation that monitored the current chart symbol.
