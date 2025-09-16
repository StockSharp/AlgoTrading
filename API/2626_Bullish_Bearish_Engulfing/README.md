# Bullish & Bearish Engulfing Strategy

## Overview
This strategy replicates the classic bullish and bearish engulfing candlestick setup that was originally implemented in MetaTrader for the "Bullish and Bearish Engulfing" expert advisor. The StockSharp port evaluates completed candles on a configurable timeframe, optionally skips a number of recent bars, and reacts when an engulfing pattern meets a minimum distance filter. The logic is designed for discretionary traders who want to automate an established price action pattern while keeping control over direction, volume, and how existing positions are handled.

## Pattern definition
An engulfing signal is confirmed when two consecutive completed candles satisfy the following rules (after applying the configured shift):

- **Bullish engulfing**
  - The most recent evaluated candle closes above its open (bullish body).
  - The preceding candle closes below its open (bearish body).
  - The bullish candle makes a higher high and lower low than the previous candle by at least the distance filter.
  - The bullish close finishes above the previous open and its open is below the prior close, again respecting the distance filter.
- **Bearish engulfing**
  - The evaluated candle closes below its open (bearish body).
  - The preceding candle closes above its open (bullish body).
  - The bearish candle still prints a higher high but closes well below the previous open, and its open exceeds the prior close, each by the distance filter.
  - The low of the bearish bar is below the previous low by the distance filter.

These conditions reproduce the original MetaTrader implementation, which demanded that the engulfing candle fully covers the previous body and extends beyond both extremes. The distance filter is measured in pips and converted to price by using the instrument's price step and decimals (5-digit and 3-digit forex quotes are automatically scaled to 10-point pips).

## Trading logic
1. Subscribe to the selected candle type through the high-level API and process only finished candles.
2. Maintain a short rolling buffer that stores the OHLC values required for the current shift value.
3. When at least two historical candles are available for evaluation, test the bullish and bearish engulfing conditions described above.
4. Upon a bullish signal, send a market order on the side defined by **BullishSide**. Upon a bearish signal, use the side configured via **BearishSide**.
5. If **CloseOppositePositions** is enabled and an opposite exposure exists, the strategy increases the order volume by the absolute current position so that the resulting trade both closes the opposite leg and opens a new one in the desired direction. When the flag is disabled, signals are ignored while an opposite position is open.
6. Position sizing is controlled by the strategy **Volume** parameter (default 1 contract/lot). No automatic stop-loss or take-profit is attached by default; risk management is left to the end user or to protective modules (you can combine it with StockSharp's built-in protections).

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `CandleType` | Timeframe (StockSharp `DataType`) used for signal detection. | 1-hour time frame | Adjustable to any supported candle type. |
| `Shift` | Number of completed candles to skip before evaluating the pattern. | 1 | Setting 1 analyses the latest closed bar, higher values look further back. |
| `DistanceInPips` | Minimum pip distance that the engulfing candle must exceed relative to the previous one. | 0 | Converted to price using the instrument price step; useful to filter small-bodied candles. |
| `CloseOppositePositions` | Whether to close an existing opposite position when a new signal fires. | `true` | Disabling skips the trade if the current exposure conflicts with the signal. |
| `BullishSide` | Order side executed on a bullish engulfing signal. | `Buy` | Can be flipped to `Sell` for contrarian behaviour. |
| `BearishSide` | Order side executed on a bearish engulfing signal. | `Sell` | Can be flipped to `Buy` to trade counter-trend setups. |
| `Volume` | Base order size. | 1 | The order volume is increased by `abs(Position)` when closing the opposite side. |

## Position management and risk
- Because orders are sent at market without protective stops, combine the strategy with additional modules (e.g., `StartProtection`) or configure external risk controls.
- The original MetaTrader code sized trades via a risk-based money manager. In this port the sizing is simplified to a direct volume parameter so that the behaviour is deterministic inside StockSharp; integrate a custom money management block if dynamic sizing is required.
- When `CloseOppositePositions` is `true`, reversals are immediate: the trade volume equals the base volume plus the absolute open position, guaranteeing a flat-to-new-direction transition.

## Files
- `CS/BullishBearishEngulfingStrategy.cs` – main C# implementation built on the high-level StockSharp strategy API.

> **Note:** No Python implementation is provided for this ID; only the C# version is included as requested.
