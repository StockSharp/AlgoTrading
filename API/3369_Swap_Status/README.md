# Swap Status Strategy

## Overview

The strategy subscribes to Level1 data for a configurable list of currency pairs and reports whether the overnight swap (swap long and swap short) is positive, negative, or zero. It mirrors the three MetaTrader scripts (`Swap.mq4`, `SwapMajorPairs.mq4`, and `SwapExoticPairs.mq4`) by exposing identical watch lists and printing human-readable labels whenever the swap status changes.

## Key differences from the MetaTrader version

1. **Data source** – StockSharp provides swap information through `Level1Fields.SwapBuy` and `Level1Fields.SwapSell`. When a broker does not supply those fields the strategy simply waits; no custom calculations are attempted.
2. **Logging** – Instead of using `Comment`, the port writes messages through `LogInfo`. Each entry includes the qualitative label (Positive/Negative/Zero) and the raw swap number so it can be compared with the trading terminal.
3. **Watch lists** – The MetaTrader code ships three separate experts. The StockSharp conversion consolidates them into one strategy with a `Preset` parameter (Primary symbol, Major pairs, Minor pairs, Exotic pairs) and allows adding extra tickers through the `Custom symbols` parameter.
4. **Deduplication** – Messages are emitted only when a status changes, preventing log spam while still reflecting live updates.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Preset` | Predefined group of symbols to inspect (Primary symbol, Major pairs, Minor pairs, Exotic pairs). | `PrimarySymbol` |
| `Custom symbols` | Optional comma-separated list of additional securities to monitor. | *(empty)* |

## How it works

1. When the strategy starts it resolves the securities from the selected preset, the optional custom list, and the primary `Strategy.Security` (for the `PrimarySymbol` mode).
2. For each resolved security it subscribes to Level1 updates and waits for the `SwapBuy` and `SwapSell` fields to arrive.
3. Once both swap values are known the strategy assigns the labels Positive, Negative, or Zero and writes the result to the log.
4. The last reported labels are cached in memory so only changes trigger a new message.

## Usage notes

- Ensure that the data provider or broker connection delivers swap information; otherwise the strategy will remain silent.
- The presets use MetaTrader-style ticker codes (`EURUSD`, `USDJPY`, etc.). Adjust them if your provider uses a different suffix scheme.
- The strategy does not submit any orders; it is purely informational and safe to run on a paper trading connection.
