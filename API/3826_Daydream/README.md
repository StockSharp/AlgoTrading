# Daydream Strategy

## Overview

The **Daydream Strategy** is a direct conversion of the MQL4 expert advisor *Daydream by Cothool*. The original robot trades the USD/JPY H1 chart by watching for breakouts of a recent price channel and then managing trades with a virtual, trailing take profit. This StockSharp port keeps the same core logic while using the high-level API: Donchian Channels deliver the breakout levels, orders are placed through `BuyMarket` / `SellMarket`, and all trailing logic is handled inside the strategy without placing actual take-profit orders at the exchange.

Key characteristics:

- Single-position breakout system that only flips direction after a candle closes outside the previous channel extremes.
- Virtual take profit measured in pips that ratchets with price to lock profits and closes trades when reached.
- Entry throttling so that only one trading action (open/close) can happen per candle, mirroring the MQL4 `LastOrderTime` restriction.

## Trading Logic

1. Build a Donchian Channel with `ChannelPeriod` completed candles and store the previous upper/lower levels.
2. When a candle closes **below** the previous lower band:
   - Close an existing short position.
   - On the next candle, open a new long position with `OrderVolume` and set the virtual take-profit level to `close + TakeProfitPips * pipSize`.
3. When a candle closes **above** the previous upper band:
   - Close an existing long position.
   - On the next candle, open a new short position and set the virtual take-profit to `close - TakeProfitPips * pipSize`.
4. While a position is active, tighten the virtual take-profit price each bar. If price hits that level on a subsequent candle, exit the trade.

The pip size is derived from the security `PriceStep`. For JPY pairs this converts a 0.001 step into a 0.01 pip increment, matching the MQL behavior.

## Parameters

| Name | Description | Default | Notes |
|------|-------------|---------|-------|
| `OrderVolume` | Volume used for each new market entry. | `1` | Matches the `Lots` input from the MQL expert. |
| `ChannelPeriod` | Number of completed candles in the Donchian Channel. | `25` | Mirrors `ChannelPeriod` in MQL. |
| `Slippage` | Allowed slippage in points. | `3` | Stored for completeness; market orders ignore it. |
| `TakeProfitPips` | Distance of the virtual take profit in pips. | `15` | Moves with price while the position is open. |
| `CandleType` | Timeframe used to build the Donchian Channel. | `1 hour` | Default timeframe of the original strategy. |

## Workflow Diagram

```
Candle closes
      │
      ├─► Update Donchian Channel (previous bands)
      │
      ├─► Breakout below previous low? ──► Close short → schedule long next bar
      │
      ├─► Breakout above previous high? ─► Close long → schedule short next bar
      │
      └─► Trail virtual take profit in the direction of the open position
              └─► Price reached virtual target? → Close position
```

## Usage Notes

- Attach the strategy to any security with streaming candles. The default settings match the original USD/JPY H1 recommendation.
- Only one position exists at a time. The strategy prevents opening and closing trades within the same candle to replicate the MQL4 logic.
- The take profit is virtual: the exit occurs through a market order once the calculated level is breached. No actual TP orders are sent to the broker.
- Adjust `CandleType` to run on different timeframes. Higher periods require sufficient historical data to warm up the Donchian Channel.

## Differences from the MQL4 Version

- Uses StockSharp `DonchianChannels` indicator instead of manually scanning highs and lows.
- Trailing take profit and action throttling are preserved, but the execution uses StockSharp market orders without relying on MT4 ticket management.
- The `Slippage` parameter is kept for parity, although market execution in StockSharp does not apply slippage the same way as MT4.

## Files

- `CS/DaydreamStrategy.cs` – strategy implementation in C#.
- Python version: not yet implemented.
