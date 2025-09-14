# HFT Spreader for FORTS

## Overview
This strategy replicates the behavior of an HFT spreader on the FORTS market. It continuously monitors the order book and places limit orders on both sides of the market to capture the bid-ask spread.

## Strategy Logic
- Subscribe to real-time order book updates.
- When no position is open and the spread is wide enough (determined by `SpreadMultiplier`), the strategy places:
  - A buy limit order one tick above the best bid.
  - A sell limit order one tick below the best ask.
- If a position exists and no active orders are present, it places a single limit order at the opposite side to close and reverse the position.
- Orders are cancelled and replaced whenever the best prices move to keep them at the top of the book.

## Parameters
- `SpreadMultiplier` – required spread in ticks to place both buy and sell orders. Default is 4 ticks.
- `Volume` – order volume. Default is 1 lot.

## Usage Notes
- Designed for instruments with small tick sizes such as futures on the FORTS exchange.
- Uses only limit orders; no market orders are sent except by the protection mechanism if needed.
- Ensure sufficient liquidity and low latency environment for effective operation.
