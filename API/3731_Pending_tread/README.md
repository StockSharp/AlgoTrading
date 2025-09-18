# Pending tread Grid Strategy

## Overview
The **Pending tread Grid Strategy** is a faithful StockSharp port of the MetaTrader 4 expert advisor `Pending_tread.mq4`. The original EA constantly rebuilds two ladders of pending orders: one ladder above the market and one below. Each ladder can be configured to use buy or sell orders, and spacing is defined in pips. The StockSharp implementation reproduces the same behaviour through the high-level API without introducing additional indicators or collections.

## Trading Logic
1. **Bid/ask driven maintenance** – the strategy subscribes to Level 1 quotes (`SubscribeLevel1`) and keeps the latest bid and ask prices. Every time new data arrives, the maintenance routine runs (with a configurable throttle) and compares the existing pending orders with the configured grid size.
2. **Above-market ladder** – depending on `AboveMarketSide`, the algorithm places either buy stop or sell limit orders at increments of `PipStep` pips above the market. Each new order receives its own take-profit level, offset by `TakeProfitPips` pips.
3. **Below-market ladder** – the `BelowMarketSide` parameter selects between buy limit and sell stop orders stacked below the market. The same pip spacing and take-profit logic applies.
4. **Stop-level guard** – the `MinStopDistancePoints` parameter emulates the MetaTrader `MODE_STOPLEVEL` check. Orders are skipped when the distance between the price and the relevant bid/ask anchor is smaller than the provided limit.
5. **Throttle** – `ThrottleSeconds` mirrors the original five-second throttle that avoided `TRADE_CONTEXT_BUSY` errors. Only one maintenance cycle is executed during that interval, regardless of how many ticks arrive.

All pip-based inputs (`PipStep`, `TakeProfitPips`) are converted into absolute price offsets using the instrument `PriceStep` and `Decimals`. Five-digit quotes automatically multiply the step by ten to match the MetaTrader "adjusted point" logic.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | 0.01 | Volume used when placing every pending order. Rounded to the instrument volume step before registration. |
| `PipStep` | 12 | Spacing between consecutive orders in the ladder, expressed in pips. |
| `TakeProfitPips` | 10 | Distance in pips used to place the take-profit for every pending order. |
| `OrdersPerSide` | 10 | Maximum number of active orders maintained above the market and below the market. |
| `AboveMarketSide` | Buy | Order type used above the market. `Buy` creates buy stop orders, `Sell` creates sell limit orders. |
| `BelowMarketSide` | Sell | Order type used below the market. `Buy` creates buy limit orders, `Sell` creates sell stop orders. |
| `MinStopDistancePoints` | 0 | Minimal distance (in raw points) allowed between the bid/ask and the pending price. Set this to the broker `MODE_STOPLEVEL` if needed. |
| `ThrottleSeconds` | 5 | Cooldown period between grid maintenance cycles. |
| `SlippagePoints` | 3 | Preserved for documentation parity; StockSharp pending orders do not use this value. |

## Implementation Notes
- Uses only the StockSharp high-level helpers (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`).
- Prices are normalised through `Security.ShrinkPrice` so that the broker receives valid tick-aligned values.
- Volume is adjusted to respect `VolumeStep`, `MinVolume`, and `MaxVolume` before each order is sent.
- All diagnostic messages are routed through `AddInfoLog` / `AddWarningLog`, mirroring the verbose output of the MetaTrader script.
- Python implementation is intentionally omitted, as requested.

## Usage Tips
1. Assign a liquid instrument and portfolio, then start the strategy. Pending ladders will appear instantly after the first level-1 update.
2. Increase `OrdersPerSide` with caution: every additional rung results in another live pending order on the broker side.
3. To mimic the original EA precisely, keep the default throttle at five seconds and configure `MinStopDistancePoints` with the broker's stop level requirement.
4. Remember that StockSharp handles net positions; if opposite ladders are triggered simultaneously, resulting fills will partially offset each other rather than create hedged sub-positions.
