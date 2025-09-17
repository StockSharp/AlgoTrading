# Change TPSL By Percentage Strategy

## Overview
This strategy replicates the MetaTrader utility that constantly adjusts take-profit and stop-loss levels for all open positions on a single symbol. Instead of modifying working orders, the StockSharp version watches level1 quotes and automatically closes positions when the floating profit or loss reaches targets derived from the account balance, used margin, and the configured leverage factor.

The strategy does not create new entries. It serves as a protective overlay that can be attached to an existing manual or algorithmic position. Whenever the calculated profit or loss thresholds are breached, the position is flattened with a market order.

## Parameters
- **Profit Percentage** – percentage of the account balance used to define the profit target. Defaults to 40% of the current balance.
- **Stop-loss Percentage** – percentage of the account balance used to define the maximum tolerable loss. Defaults to 90% of the current balance.
- **Symbol Leverage** – leverage coefficient that transforms the percentage target into a price distance. Example: with 1:200 leverage, enter `0.5` just like the original script.

## How Targets Are Calculated
1. Read the current account balance (portfolio current value) and the blocked margin that is already engaged in positions.
2. Compute the monetary profit and loss targets: `balance × percentage ÷ 100`.
3. Divide each target by the used margin to obtain multipliers that describe how much price movement is required to reach the desired outcome.
4. Multiply the multipliers by the leverage factor and convert them into price coefficients:
   - Long take-profit price = `entry price × (1 + leverage × profitMultiplier ÷ 100)`
   - Long stop-loss price = `entry price × (1 − leverage × stopMultiplier ÷ 100)`
   - Short take-profit price = `entry price × (1 − leverage × profitMultiplier ÷ 100)`
   - Short stop-loss price = `entry price × (1 + leverage × stopMultiplier ÷ 100)`
5. These thresholds are updated on every level1 change so that adjustments in balance or margin are reflected immediately.

## Execution Flow
1. Subscribe to level1 data to receive best bid/ask updates.
2. Skip processing until the strategy is online, a position exists, and both margin and balance are available.
3. For long positions, monitor the bid price; for short positions, monitor the ask price.
4. When the observed price crosses the computed take-profit or stop-loss level, send a market order that exits the entire position volume.
5. Once the position is closed the strategy waits for the next position to appear before recalculating thresholds.

## Usage Notes
- Attach the strategy to a single symbol. The original indicator was not designed to manage multiple instruments simultaneously.
- If no margin information is available the logic remains idle, mirroring the MetaTrader behavior when AccountMargin equals zero.
- Negative stop levels are clamped to zero to avoid sending nonsensical prices on instruments that cannot trade below zero.
- Because exits are triggered with market orders, slippage can occur when volatility is high.
- The strategy is intended to complement other entry systems; it never generates trade signals on its own.
