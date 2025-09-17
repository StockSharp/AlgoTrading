# Secwenta Multi-Bar Signals Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor "Secwenta" (MQL ID 22977). The algorithm scans completed candles and counts how many of them closed bullish (close > open) or bearish (close < open) within a short rolling history. Depending on the configuration it can operate in buy-only, sell-only, or two-way reversal modes. When the required number of bullish or bearish bars appears, the strategy opens or closes market positions using a fixed volume that mirrors the original lot setting.

## Signal evaluation
- Only finished candles from the selected `CandleType` are processed via the high-level subscription API.
- For each candle the strategy records whether it was bullish, bearish, or neutral (doji). The internal buffer keeps the latest *N* directions, where *N* is the larger of `BullishBarCount` and `BearishBarCount` among the enabled sides (buy and/or sell).
- The bullish counter increments whenever a candle closes above its open, while the bearish counter increments on closes below the open. Neutral candles do not affect the counters.
- A signal is triggered once the corresponding counter reaches its configured threshold inside the rolling window. This reproduces the original MQL logic that iterated through the most recent bars until the requested number of bull or bear candles was found.

## Trading rules
1. **Buy-only mode (`UseBuySignals = true`, `UseSellSignals = false`):**
   - When the bearish counter reaches `BearishBarCount`, any existing long position is closed with a market sell order.
   - When the bullish counter reaches `BullishBarCount` and the strategy is flat, a new long position is opened using `OrderVolume`.
2. **Sell-only mode (`UseBuySignals = false`, `UseSellSignals = true`):**
   - When the bullish counter reaches `BullishBarCount`, an open short position is covered with a market buy order.
   - When the bearish counter reaches `BearishBarCount` and the strategy is flat, a new short position is opened using `OrderVolume`.
3. **Reversal mode (`UseBuySignals = true` and `UseSellSignals = true`):**
   - A bullish trigger closes any short exposure and, if the strategy is not already long, opens a new long by buying `OrderVolume` plus the absolute size of the short position. This mimics the original sequence of closing sells before opening buys.
   - A bearish trigger closes any long exposure and, if the strategy is not already short, opens a new short by selling `OrderVolume` plus the absolute size of the long position.

All market operations reuse StockSharp's `BuyMarket` and `SellMarket` helpers, and the strategy calls `StartProtection()` so that account-level protections can be layered on top if desired.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `CandleType` | Candle data type (time frame) used for evaluating sequences. | 1-hour time frame | Any StockSharp-supported candle type can be selected. |
| `OrderVolume` | Base market order volume that mirrors the MQL lot size. | 1 | Added to the closing volume when reversing a position. |
| `UseBuySignals` | Enables bullish signal processing. | `true` | When disabled, no new long trades are opened. |
| `BullishBarCount` | Number of bullish candles required to trigger a bullish event. | 2 | Should stay consistent with the closing threshold when running buy-only mode. |
| `UseSellSignals` | Enables bearish signal processing. | `false` | When disabled, no new short trades are opened. |
| `BearishBarCount` | Number of bearish candles required to trigger a bearish event. | 1 | Acts both as the opening threshold for shorts and the exit threshold for longs. |

## Implementation notes
- The rolling window uses a queue to keep the latest candle directions and ensures the counters match the size of the window even after parameter changes.
- Only finished candles are processed to remain faithful to the original "new bar" event handling.
- Neutral (doji) candles leave the counters unchanged, exactly as in the MQL code.
- Reversals are executed with a single market order that combines the closing and opening volume, maintaining deterministic exposure changes.
- The buffer length equals the largest active threshold; if one side is disabled, only the corresponding threshold contributes to the lookback length, matching the behaviour of `CopyRates` in the MQL version.

## Files
- `CS/SecwentaMultiBarSignalsStrategy.cs` – main C# implementation built on the StockSharp high-level strategy API.

> **Note:** No Python translation is supplied for this ID; only the requested C# version is provided.
