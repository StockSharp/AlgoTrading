# Visual Trader Simulator Edition
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy is a simplified port of the VisualTrader scripts from MetaTrader.

It opens a single market position in the chosen direction and attaches protective stop-loss and take-profit orders. Parameters allow configuring direction, take profit, and stop loss in absolute price values. The strategy demonstrates how manual trade management scripts can be recreated using StockSharp high-level API.

## Parameters

- **Trade Direction** – choose Buy or Sell for the initial order.
- **Take Profit** – optional take profit value in absolute price. Set to 0 to disable.
- **Stop Loss** – optional stop loss value in absolute price. Set to 0 to disable.
- **Volume** – base strategy volume used for the market order.

## Trading Logic

On start the strategy:

1. Creates protective orders using `StartProtection`.
2. Sends a market order based on the selected trade direction.

The example does not rely on indicators or market data and is intended for demonstration purposes.
