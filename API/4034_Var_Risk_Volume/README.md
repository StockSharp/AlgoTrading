# Var Risk Volume Strategy

## Overview
This strategy ports the MetaTrader 4 script `ACB4.MQ4` (function `amd.OperationVolume`) to the StockSharp high-level API. The original code computed the largest position volume that keeps the portfolio's loss below a Value-at-Risk (VaR) threshold for a given distance in points. The C# version reproduces the same arithmetic with StockSharp security metadata and writes the resulting lot size into `Strategy.Volume` for reuse by other components.

Unlike classic automated trading systems, the strategy does not place orders. It acts as a risk management helper that inspects instrument parameters (minimum volume, volume step, tick value, margin requirements and price steps) and calculates the maximum position size that satisfies the configured VaR scenario. When the calculation succeeds, the result is logged and exposed through dedicated properties.

## Parameters
- **VaR Limit** – maximum allowed loss in account currency. Mirrors `avd.VARLimit` in the MQL file.
- **VaR Points** – number of points used in the VaR scenario (distance between entry and loss level). Mirrors `aai.VARPoints`.
- **Log Details** – when enabled, the strategy prints all intermediate values, matching the verbose `Alert` output of the script.

All parameters support optimization metadata so the sizing method can be stress-tested from the StockSharp UI.

## Calculation Steps
The conversion stays faithful to the original procedure:
1. Fetch instrument properties: `MinVolume`, `VolumeStep`, `MarginBuy`/`MarginSell`, `StepPrice`, `PriceStep` and `MinPriceStep`.
2. Derive minimal margin, minimal tick value and minimal point value for the smallest tradable volume.
3. Compute how many points are required to offset the minimal margin and translate the configured **VaR Points** into the number of positions that can fit into the loss budget.
4. Convert the remaining margin into a raw volume limit and snap it to the exchange volume step. If the exchange reports a `MaxVolume`, the value is capped accordingly.
5. Store the final lot size inside `OperationVolume` and assign it to `Strategy.Volume`.

Whenever a required market parameter is missing (for example the broker does not publish `MinVolume` or `StepPrice`), the strategy keeps a list of missing fields, raises a warning and sets the operation volume to zero.

## Logging and Diagnostics
With **Log Details** enabled the strategy emits a block of log messages identical to the `Alert` sequence from the MT4 script. It includes:
- Intermediate metrics such as minimal margin, minimal tick value, minimal point value and the computed volume limit.
- The original inputs (symbol identifier, VaR limit, VaR points and portfolio currency).
- Portfolio snapshot (current and starting value) when available.

The `MissingFields` property exposes the names of absent security parameters so integrators can troubleshoot broker data feeds.

## Usage
1. Assign the strategy to a security and portfolio that provide margin and tick information.
2. Set **VaR Limit** and **VaR Points** according to the desired loss tolerance.
3. Start the strategy. It immediately performs the calculation, logs the result and calls `StartProtection()` to keep the standard defensive pattern used across the repository.
4. Read the suggested lot size from `OperationVolume` or from `Strategy.Volume` and apply it in other trading algorithms.

If the log reports missing parameters, review the instrument configuration inside StockSharp or request the necessary metadata from the broker. Without complete margin and tick data the VaR-based position sizing cannot be performed.

## Implementation Notes
- All indentation uses tabs to respect repository guidelines, and inline comments are written in English only.
- The implementation relies solely on the high-level API and does not create custom data collections.
- No candles or tick subscriptions are required; the calculation runs once during `OnStarted`. Manual recalculation can be triggered by resetting or restarting the strategy after parameter changes.
