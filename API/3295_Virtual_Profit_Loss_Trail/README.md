# Virtual Profit/Loss Trail Strategy

## Overview

`VirtualProfitLossTrailStrategy` reproduces the behaviour of the MetaTrader expert advisor "Virtual Profit Loss Trail" inside StockSharp. The strategy never opens new positions by itself. Instead, it continuously supervises the current position of the selected security and applies protective logic:

- A configurable take-profit distance expressed in pips.
- A configurable stop-loss distance expressed in pips.
- A virtual trailing stop that activates after a minimum profit is reached and slides with the market only when price advances by the specified trailing step.

Because the protective levels are virtual, no actual stop or limit orders are sent to the exchange. The strategy monitors best bid/ask updates and closes the open position with a market order when any of the virtual levels is touched.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Take-profit (pips)** | Distance between the entry price and the profit target. Set to `0` to disable the take-profit exit. |
| **Stop-loss (pips)** | Distance between the entry price and the protective stop. Set to `0` to disable the stop-loss exit. |
| **Trailing stop (pips)** | Distance used to compute the trailing stop. When set to `0` the trailing logic is disabled entirely. |
| **Trailing step (pips)** | Additional profit that must be gained before the trailing stop is shifted further. Use `0` to move the trail every time a new high/low is printed. |
| **Trailing activation (pips)** | Minimum profit that must be locked before the trailing stop becomes active. When set to `0`, trailing starts immediately after entering the position. |

All distances are measured in pip units. The strategy automatically derives the pip size from the security’s price step: for symbols with three or five decimal places a pip is defined as ten price steps, otherwise one step.

## Logic

1. **Market data subscription** – the strategy subscribes to Level1 data to receive best bid and best ask updates. Only finished updates are processed, ensuring the algorithm works both in real time and during historical replays.
2. **Long position management** – when the net position is long, the strategy calculates the virtual stop-loss, take-profit, and trailing stop levels based on the average entry price. If the best bid touches the stop-loss or take-profit the position is closed immediately. Once the activation profit is achieved, the trailing stop follows the price upward. The stop is only advanced when the trailing step requirement is satisfied.
3. **Short position management** – the same logic is applied symmetrically using the best ask for exits from short positions.
4. **Reset behaviour** – whenever the position is fully closed, internal trailing references are reset to prevent accidental re-entry signals.

## Usage Tips

- Attach the strategy to a connector and security that already has an open position or will receive orders from other strategies or manual trading. The manager will control the aggregate position size.
- Ensure Level1 data is available; without current bid/ask values the virtual levels cannot be evaluated.
- The strategy can be combined with any entry-generating strategy by running both under the same portfolio and security. Only one instance should manage the protective logic to avoid conflicts.

## Differences from the MQL Expert

- The StockSharp version works with aggregated positions rather than individual order tickets. It automatically calculates the average entry price provided by the platform.
- Visual line drawing and sound alerts from the original expert are replaced by logging within StockSharp. Protective actions are visible in the strategy journal.
- The same pip-based configuration is preserved, including the trailing activation threshold and incremental trailing step.

## Files

- `CS/VirtualProfitLossTrailStrategy.cs` – C# implementation of the strategy.
- `README.md` – this documentation.
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.
