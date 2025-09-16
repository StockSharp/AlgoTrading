# Simple Copier Strategy

## Overview
SimpleCopierStrategy synchronizes orders between accounts using a CSV file. The strategy operates in two modes:

- **Master** – writes all active orders to `C4F.csv` every second.
- **Slave** – reads `C4F.csv` and replicates orders on the current account.

## Parameters
- **Mode** – working mode of the copier.
- **Slippage** – allowed price slippage in pips.
- **Multiplier** – volume multiplier used in slave mode.

## How It Works
1. On a timer, the strategy processes the file according to the selected mode.
2. Master mode outputs active orders excluding those already copied.
3. Slave mode opens missing orders, adjusts volumes by the multiplier and cancels orders that disappeared from the file.

The sample supports both market and pending orders and copies stop-loss and take-profit levels if they are present.

