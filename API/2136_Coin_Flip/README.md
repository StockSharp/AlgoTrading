# Coin Flip Strategy

## Overview

The **Coin Flip Strategy** randomly chooses to go long or short on each new candle when no position is open. After closing a position, if the trade ended in loss, the next trade size is increased using a martingale multiplier. The strategy closes positions using fixed take‑profit and stop‑loss levels defined in price steps and can optionally trail profits after a specified distance.

## Parameters

- `Volume` – base order size used for the first attempt.
- `Martingale` – multiplier applied to the volume after a losing trade.
- `MaxVolume` – upper limit for the position size after martingale increases.
- `TakeProfit` – profit target in price steps.
- `StopLoss` – loss limit in price steps.
- `TrailingStart` – distance in price steps where trailing becomes active.
- `TrailingStop` – trailing stop distance in price steps.
- `CandleType` – time frame of candles used for decision making.

## How It Works

1. On each finished candle, the strategy checks for an open position.
2. If a position exists, it monitors profit or loss using the current close price. Once take‑profit, stop‑loss, or trailing stop conditions are met, the position is closed.
3. When no position is open, a virtual coin is flipped:
   - Heads opens a long position.
   - Tails opens a short position.
4. If the previous trade was a loss, the volume is multiplied by `Martingale` but capped by `MaxVolume`.
5. Trailing stop is engaged once price moves by `TrailingStart` in the favorable direction.

## Notes

This example is intended for educational purposes to demonstrate how to work with random signals and position sizing using the StockSharp high‑level API.
