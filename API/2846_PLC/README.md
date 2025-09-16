# PLC Strategy

## Overview

The PLC strategy replicates the behavior of the MetaTrader expert advisor `PLC (barabashkakvn's edition)` using the StockSharp high-level API. The algorithm operates on the high timeframe specified by the `Entry Timeframe` parameter and places breakout stop orders above and below the most recent finished candle. Lower timeframe fractals (M5 and H1 by default) are used to dynamically scale the order volume. Once the floating profit of all open positions exceeds the configured threshold, the strategy liquidates the entire position and waits for the next setup.

## Trading Logic

1. **New candle processing** – the strategy reacts only when a candle is fully closed on the main timeframe. All calculations are performed with the closed bar data to avoid repainting.
2. **Order/position maintenance** – before evaluating a new setup the algorithm cancels pending stop orders scheduled for deletion and closes positions when the profit target was reached on a previous bar.
3. **Price offsets** – the high and low of the last finished candle are shifted by the number of pips configured via `Shift OHLC`. The pip size is automatically adjusted for 3- or 5-digit forex symbols.
4. **Fractal updates** – dedicated subscriptions track fractal patterns on the M5 and H1 timeframes. The most recent upward and downward fractal values are stored whenever a classic five-bar pattern is completed.
5. **Distance check** – a new buy stop is placed only if the shifted high is at least `Shift Position` pips above the highest entry price of open long trades, or if there are no long trades and no active buy stops. The same rule with inverted comparisons applies to sell stops.
6. **Dynamic lot sizing** – the base volume (`Buy Volume` or `Sell Volume`) is multiplied by the M5 or H1 multiplier when the stop level breaks above the corresponding fractal. Setting a multiplier to zero disables the scaling for that timeframe.
7. **Order registration** – stop orders are sent via `BuyStop`/`SellStop`. References to the registered orders are tracked to simplify later cancellation.
8. **Profit supervision** – after summing open profit of all long and short lots (using the instrument’s step value) the strategy toggles the `close positions` mode once the profit exceeds `Minimum Profit`. Market orders are used on the next bar to flatten the exposure.
9. **Trade feedback** – when a pending stop order is executed, every other pending stop is cancelled to mimic the original MQL logic.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Shift OHLC` | Number of pips added above the last candle high and below the last candle low to determine the stop activation levels. |
| `Minimum Profit` | Profit (in the instrument currency) that triggers closing all open positions. |
| `Shift Position` | Minimum distance in pips between the new stop level and the extreme open-price of existing positions. Prevents stacking orders too close to previous entries. |
| `Buy Volume` / `Sell Volume` | Base order size (lots). Used before any fractal multipliers are applied. |
| `M5 Multiplier` / `H1 Multiplier` | Volume multipliers activated when the stop price is above (for longs) or below (for shorts) the latest fractal on the respective timeframe. Use `0` to disable scaling. |
| `Entry Timeframe` | Main timeframe used to generate entries. Each finished candle on this timeframe triggers a new evaluation. |
| `M5 Fractal Timeframe` | Timeframe that feeds the lower fractal detector (default 5 minutes). |
| `H1 Fractal Timeframe` | Timeframe that feeds the higher fractal detector (default 1 hour). |

## Position Management

- **Cancellation** – The strategy keeps references to all pending stop orders. When a stop order is filled, every remaining pending order is cancelled on the next evaluation cycle.
- **Flattening** – When `Minimum Profit` is exceeded, the net position is flattened using market orders (`SellMarket` for longs, `BuyMarket` for shorts). The flag is cleared once the position size returns to zero.
- **Inventory tracking** – Filled orders are recorded as individual lots to replicate the MetaTrader behavior that differentiates between the highest buy and lowest sell entry prices.

## Notes

- The default parameters mirror the original expert advisor configuration. You can switch the fractal timeframes by editing `M5 Fractal Timeframe` and `H1 Fractal Timeframe` parameters if the instrument requires different context windows.
- Volumes are rounded down to the exchange volume step before sending orders. If the resulting value is zero the order is skipped.
- The profit calculation uses the instrument’s price and step value to stay compatible with instruments that have non-unit tick value.

