# Martingale Trade Simulator Strategy

## Overview

`MartingaleTradeSimulatorStrategy` recreates the "Martingale Trade Simulator" expert advisor from MetaTrader inside the StockSharp framework. The strategy is a manual trading panel that lets a trader send immediate market orders, apply martingale-style averaging, and manage trailing protection without scripting additional automation. It reacts to parameter switches in real time, making it suitable for Strategy Tester experiments just like the original MQL robot.

## How it works

### Manual market buttons
- `Buy` and `Sell` parameters act as virtual buttons. When either parameter is set to `true`, the strategy sends a market order with volume `Order Volume` and then automatically resets the parameter to `false`.
- No pending orders are used — the strategy works entirely with market executions, mirroring the simulator behavior inside MetaTrader's visual tester.

### Martingale averaging
- Enabling `Enable Martingale` allows the panel to place averaging orders when the `Martingale` parameter is toggled to `true`.
- The strategy checks the active position:
  - **Long position:** If the current ask price is at least `Martingale Step (points)` below the lowest filled buy price, a new buy order is sent.
  - **Short position:** If the current bid price is at least `Martingale Step (points)` above the highest filled sell price, a new sell order is issued.
- Each averaging order volume equals `Order Volume × Martingale Multiplier^N`, where `N` is the number of consecutive entries in the current direction.
- When martingale is active, the take-profit target is recalculated to the weighted average entry price plus/minus `Martingale TP Offset (points)` to cover accumulated drawdown.

### Trailing stop module
- `Enable Trailing` activates a protective trailing stop that follows the most recent best price.
- The trailing stop starts at `Trailing Stop (points)` away from the market price and moves forward only after price improves by at least `Trailing Step (points)`.
- If the market price crosses the trailing level, the strategy immediately closes the entire position with an opposing market order.

### Stop-loss and take-profit
- `Stop Loss (points)` and `Take Profit (points)` reproduce the basic risk controls from the original expert advisor.
- For long positions the stop is placed below the average entry price, while the take-profit sits above. For short positions both levels are mirrored.
- Protective exits are executed with market orders, so the strategy stays compatible with any connector supported by StockSharp.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `Order Volume` | Base size for manual market orders. | `1` |
| `Stop Loss (points)` | Distance to the protective stop. Zero disables the stop-loss. | `500` |
| `Take Profit (points)` | Distance to the protective target. Zero disables the take-profit. | `500` |
| `Enable Trailing` | Turns the trailing stop module on/off. | `true` |
| `Trailing Stop (points)` | Distance between price and trailing stop. | `50` |
| `Trailing Step (points)` | Minimal favorable move required to advance the trailing stop. | `20` |
| `Enable Martingale` | Allows averaging orders controlled by the `Martingale` button. | `true` |
| `Martingale Multiplier` | Volume multiplier used for each additional averaging trade. | `1.2` |
| `Martingale Step (points)` | Required adverse movement before an averaging order is allowed. | `150` |
| `Martingale TP Offset (points)` | Additional offset applied to the averaged take-profit level. | `50` |
| `Buy` | Set to `true` to send a market buy order (auto-resets). | `false` |
| `Sell` | Set to `true` to send a market sell order (auto-resets). | `false` |
| `Martingale` | Set to `true` to evaluate and place an averaging order (auto-resets). | `false` |

## Usage tips

1. Attach the strategy to an instrument, set `Order Volume`, and start it in tester or live mode.
2. Use the `Buy` / `Sell` toggles to simulate button clicks from the MetaTrader panel.
3. After the first trade, trigger the `Martingale` toggle whenever the price moves against the position. The strategy verifies the price distance and increases the volume if conditions are met.
4. Adjust trailing and risk parameters to replicate the original EA's behavior or to experiment with alternative settings.

## Notes

- The strategy relies on Level1 data (best bid/ask and last trade) to evaluate market conditions.
- All comments inside the C# code are in English, keeping consistency with the repository guidelines.
