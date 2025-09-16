# Signals Demo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Example strategy that demonstrates how to subscribe and unsubscribe to external trading signals.
The strategy manages basic risk parameters before initiating the subscription.

## Details

- **Purpose**: showcase signal subscription workflow in StockSharp.
- **Trading**: the strategy does not execute trades; it only logs subscription actions.
- **Parameters**:
  - `SignalId` – identifier of the signal to follow.
  - `EquityLimit` – maximum equity that can be used for copying the signal.
  - `Slippage` – allowed price slippage when replicating trades.
  - `DepositPercent` – percentage of account equity allocated to the signal.
- **Default Values**:
  - `SignalId` = 0
  - `EquityLimit` = 0
  - `Slippage` = 2
  - `DepositPercent` = 5
- **Validation**:
  - `DepositPercent` is forced into the range 5–95.
  - Negative `EquityLimit` and `Slippage` are reset to 0.

## Usage

1. Configure the parameters in the UI.
2. Start the strategy. It will validate parameters and log the subscription.
3. Stop the strategy to unsubscribe from the signal.

This sample is intended as a learning tool and does not implement actual trading logic.
