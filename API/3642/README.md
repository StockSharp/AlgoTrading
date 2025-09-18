# Martingale Trade Simulator Strategy

## Overview

The **Martingale Trade Simulator Strategy** replicates the manual MetaTrader 5 expert advisor "Martingale Trade Simulator" using the StockSharp high level API. The strategy starts with a single market order in a user-defined direction and then manages the position automatically:

- Adds new martingale legs when price moves adversely by a configurable step.
- Recalculates take-profit levels to exit the entire basket near break-even plus a safety buffer.
- Applies a trailing-stop block that mimics the original EA behaviour.
- Works on candle updates from any timeframe provided by the `CandleType` parameter.

This conversion keeps the risk controls and position-management workflow of the original tester utility while allowing it to be automated inside the StockSharp infrastructure.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `InitialVolume` | Base lot size for the very first trade. | `0.01` |
| `StopLossPips` | Protective stop-loss distance in pips from the averaged entry. | `500` |
| `TakeProfitPips` | Take-profit distance applied while only a single position exists. | `500` |
| `TrailingStopPips` | Distance used to activate trailing once the trade is in profit. | `50` |
| `TrailingStepPips` | Step used to move the trailing stop after activation. | `20` |
| `LotMultiplier` | Martingale multiplier applied to every additional averaging order. | `1.2` |
| `StepPips` | Minimum adverse move (in pips) before a new averaging order is sent. | `150` |
| `TakeProfitOffsetPips` | Alternative take-profit offset used when more than one leg is open. | `50` |
| `EnableMartingale` | Enables or disables averaging additions. | `true` |
| `EnableTrailing` | Enables or disables trailing-stop management. | `true` |
| `InitialDirection` | Direction of the very first trade (`None`, `Buy`, `Sell`). | `None` |
| `CandleType` | Candle series used to drive all management actions. | `1 minute` |

## Trading Logic

1. **Initial entry** – When the strategy starts it places a market order in the direction defined by `InitialDirection`. If `None` is selected the user can trigger the first order manually.
2. **Martingale additions** – Whenever the market moves against the open position by `StepPips`, a new order is added with volume scaled by `LotMultiplier^n`. Each addition shifts the `TakeProfitOffsetPips` target so the whole basket is closed slightly in profit.
3. **Trailing block** – After the price moves in favour of the position by `TrailingStopPips` the stop-loss is pulled to lock in gains and stepped further using `TrailingStepPips`.
4. **Stop-loss and take-profit** – The strategy tracks synthetic stop-loss and take-profit levels for the whole basket and closes all positions once either level is reached.

## Usage Notes

- The strategy works best on netting accounts where only one aggregated position per symbol exists. It emulates the behaviour of the MetaTrader tester, so it is designed for experimentation rather than production-grade risk control.
- Always verify that the security has appropriate `PriceStep`, `VolumeStep`, `VolumeMin`, and `VolumeMax` values because they are used to normalise both price offsets and volumes.
- `CandleType` can be changed to any timeframe. Shorter intervals offer behaviour similar to tick updates, while longer intervals slow down management actions.

## Visualisation

When a chart area is available the strategy plots the requested candles and marks executed trades, which mirrors the visual feedback provided by the original tester panel.

## Conversion Notes

- Manual tester buttons from the MQL version were replaced with the `InitialDirection` parameter.
- Money management helpers (`CheckMoneyForTrade`, `lotAdjust`, etc.) were mapped to StockSharp's built-in volume normalisation.
- Trailing and averaging logic was refactored to operate on the aggregated position that StockSharp strategies maintain.
