# TurnGrid Strategy

## Overview

The **TurnGrid Strategy** replicates the behaviour of the original MQL5 Expert Advisor `TurnGrid.mq5`. It constructs a symmetric price grid around the current market price and alternates between long and short orders whenever the price migrates from one grid cell to another. The strategy continuously rebalances open orders to maintain both bullish and bearish exposure until the configured equity target is achieved.

The conversion uses StockSharp's high-level API: candle subscriptions drive the grid updates, market orders handle entries and exits, and risk management is expressed through strategy parameters. All comments have been translated into English and the naming follows StockSharp conventions.

## Trading Logic

1. When the strategy starts it captures the latest candle close and builds a grid containing `4 * GridShares` levels. The central level is set to the current price, upper levels scale by `1 + GridDistance`, and lower levels scale by `1 - GridDistance`.
2. An initial market buy order is placed at the centre of the grid. Its volume is calculated from the available budget portion (`Balance / GridShares`) and an incremental stake formula inherited from the MQL version.
3. Every finished candle updates the current grid index based on the close price. If the index changes:
   - Positions linked to tickets two levels away from the new index are closed (buy tickets below the price are sold, sell tickets above are bought back).
   - New positions are opened to keep both long and short anchors on the active level. If neither side is present, the strategy opens the side with fewer active positions to balance exposure.
4. Fees are approximated via the `FeeRate` parameter. Each filled order contributes to a running fee total used when evaluating performance.
5. When the account equity (after subtracting the accumulated fee estimate) exceeds the initial balance by `EquityTakeProfit`, the strategy closes the net position and rebuilds the grid around the latest price.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `GridDistance` | Relative distance between adjacent grid levels. | `0.01` |
| `GridShares` | Maximum number of concurrent grid positions that can be active. | `50` |
| `EquityTakeProfit` | Percentage gain over the initial balance required to reset the grid. | `0.02` |
| `FeeRate` | Estimated transaction fee per trade, applied to executed volume. | `0.0008` |
| `CandleType` | Candle series used to drive the strategy. | `1` minute timeframe |

## Implementation Notes

- Candle subscription is handled via `SubscribeCandles(CandleType)` and the strategy reacts only to finished candles, matching the tick-driven logic of the original EA while keeping compatibility with StockSharp.
- The grid state is stored in a lightweight array of `GridLevel` structs containing price anchors, boolean flags, and ticket volumes for deferred closures.
- Order sizes follow the original incremental capital allocation formula, with additional normalization through the security's `VolumeStep`, `VolumeMin`, and `VolumeMax` settings.
- Equity-based resets wait for the current net position to close before rebuilding the grid, ensuring clean transitions between trading cycles.

## Files

- `CS/TurnGridStrategy.cs` – C# implementation of the strategy using StockSharp high-level constructs.
- `README.md` – English documentation (this file).
- `README_cn.md` – Simplified Chinese documentation.
- `README_ru.md` – Russian documentation.
