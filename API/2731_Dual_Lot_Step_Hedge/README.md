# Dual Lot Step Hedge Strategy

## Overview

The **Dual Lot Step Hedge Strategy** is a C# port of the MetaTrader 5 expert advisors *"x1 lot from high to low"* and *"x1 lot from low to high"* (folder `MQL/19543`). The original robots immediately open a hedged basket of buy and sell positions, cycle the order volume after every new entry, and close the entire basket once a fixed profit target is reached. This implementation reproduces that behaviour on top of the StockSharp high-level API while exposing clean parameters and detailed state management.

Two operating modes are available:

- **HighToLow** – starts with the maximum lot multiplier, opens the first hedged basket with the largest volume, and then decreases to the next lot step after the first entries.
- **LowToHigh** – begins with the minimal lot step, increases the lot size after every new entry until the configured multiplier is reached, and then keeps trading at that size.

The strategy keeps both buy and sell legs alive simultaneously, manages stop-loss and take-profit levels per leg, and monitors the portfolio equity to enforce a basket-wide profit target.

## Trading Logic

1. When no positions exist the strategy opens **both** a long and a short market order using the current lot size.
2. If exactly one leg is active (for example, the opposite side was stopped out), the missing leg is re-opened at market with the current lot size.
3. After every successful entry the lot size is updated depending on the selected mode (`HighToLow` or `LowToHigh`).
4. Per-leg protective exits are evaluated on every incoming trade tick:
   - A long leg is closed if price reaches its stop-loss (`StopLossPips` below the average long entry) or its take-profit (`TakeProfitPips` above the average entry).
   - A short leg is closed if price reaches its stop-loss (`StopLossPips` above the average short entry) or its take-profit (`TakeProfitPips` below the average entry).
5. Once the portfolio equity gain exceeds `MinProfit`, the strategy closes all remaining positions and resets the lot state to the mode’s starting size.
6. Safety logic closes the basket and resets everything if more than one buy or sell position is unexpectedly detected.

All orders are submitted via the high-level `BuyMarket` and `SellMarket` helpers. The strategy tracks fills with `OnOwnTradeReceived`, maintains aggregated exposure per leg, and prevents duplicate orders while entries or exits are still pending.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `LotMultiplier` | Maximum lot multiplier expressed in minimal volume steps (default `10`). |
| `StopLossPips` | Stop-loss distance in pips for each leg (default `50`). Set to `0` to disable. |
| `TakeProfitPips` | Take-profit distance in pips for each leg (default `150`). Set to `0` to disable. |
| `MinProfit` | Basket profit target in account currency. Once the equity gain exceeds this value all positions are closed (default `27`). |
| `ScalingMode` | Lot stepping behaviour. `HighToLow` mirrors the “x1 lot from high to low” EA, `LowToHigh` mirrors “x1 lot from low to high”. |

The strategy automatically derives the minimal volume step from `Security.VolumeStep` and computes pip value using the security price step (with the traditional 4/5-digit forex adjustment).

## Reset and Volume Cycling

- **HighToLow** – opens the first basket with the highest volume (`VolumeStep * LotMultiplier`). After any entry the internal volume is reduced by one step. When the basket profit target is reached, the volume is reset to `0` so the next cycle starts from the maximum again.
- **LowToHigh** – starts from the minimal lot step. After each entry the lot is increased by one step until the multiplier ceiling is reached. When the basket profit target is hit the volume is reset to the minimal step.

## Usage Notes

- The strategy subscribes to tick trades (`DataType.Ticks`) because the original MetaTrader bots run on tick events. Configure the history provider or live connector accordingly.
- Stop-loss and take-profit checks happen inside the algorithm, so no additional protective orders are registered on the exchange.
- Because both legs are opened at market, the strategy performs best on brokers that support hedged positions and small spreads. On netting venues it will still function but legs effectively offset each other until one of them is closed by the internal logic.
- The default parameters copy the original MQL settings. Adjust them carefully: hedging high volumes can generate significant drawdowns before the basket profit target is met.

## Mapping to the Original MQL Logic

| MetaTrader Variable | C# Property / Behaviour |
|---------------------|-------------------------|
| `InpLots` | `LotMultiplier` with automatic volume-step handling. |
| `InpStopLoss` & `InpTakeProfit` | `StopLossPips` and `TakeProfitPips` with pip conversion based on `PriceStep`. |
| `InpMinProfit` | `MinProfit` and the portfolio equity check. |
| `LotCheck` | `LotCheck` helper that enforces the minimum step and maximum volume. |
| `CalculatePositions` | Internal long/short exposure tracking through `OnOwnTradeReceived`. |
| `CloseAllPositions()` | `CloseAllPositions` method with pending-order coordination and state reset. |

## Risk Management Considerations

The strategy intentionally keeps both long and short positions open, which causes continuous exposure to spread costs and swap rates. Before running on real capital:

- Validate the behaviour in the StockSharp emulator or in paper trading.
- Ensure your broker supports hedging; otherwise the long/short legs will be netted immediately.
- Tune the stop-loss, take-profit, and profit target values to the instrument’s volatility.
- Monitor margin usage, because simultaneous long/short legs double the nominal exposure.

## Files

- `CS/DualLotStepHedgeStrategy.cs` – StockSharp strategy implementation with extensive inline comments.
- `README_ru.md` – Russian translation with detailed instructions.
- `README_cn.md` – Chinese translation with detailed instructions.
