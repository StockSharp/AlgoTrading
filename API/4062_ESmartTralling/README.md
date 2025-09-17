# e-Smart Tralling Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview

The **e-Smart Tralling** strategy is a direct port of the MetaTrader expert adviser with the same name. It does not open positions on its own. Instead, it watches the currently open position for the configured security and manages it using a staged break-even and trailing-stop model that was popular in the original EA. The implementation relies purely on the high-level StockSharp API and reads price data from the configured candle subscription.

Key capabilities:

- Works for both long and short positions.
- Converts MetaTrader "points" to price increments using `Security.PriceStep` (with `Security.Step` as a fallback).
- Offers three configurable profit checkpoints that move the stop-loss closer to price.
- Optionally closes one third of the position (rounded to the nearest volume step) when the first checkpoint is reached.
- Activates a trailing stop once price exceeds the third checkpoint by an additional offset.
- Stores the calculated stop price internally and closes the position with market orders once the candle range touches that price.

> **Important:** because the original EA processed all orders in the terminal, the StockSharp version assumes that only one net position is handled by this strategy instance. Run separate instances if multiple symbols must be managed at once.

## Trade Management Workflow

1. **Initial state** – the strategy records the average entry price that StockSharp reports for the current position. No orders are registered automatically.
2. **Profit checkpoints** – as soon as the candle high (for longs) or low (for shorts) produces profits beyond `LevelProfit1`, `LevelProfit2`, or `LevelProfit3`, the stop price is moved to `LevelMovingX` points above/below the entry price.
3. **Partial profit taking** – if `UseCloseOneThird` is enabled, the strategy sends a market order that closes approximately one third of the current position when the first checkpoint is reached. The volume is rounded up to the nearest `Security.VolumeStep` (fallback `0.1`).
4. **Trailing stop** – once the unrealized profit exceeds `LevelMoving3 + TrailingStop + TrailingStep`, the stop price follows price at a distance of `TrailingStop` points. Additional movement of at least `TrailingStep` points is required before the stop is advanced again.
5. **Exit** – when the candle range falls back to or through the stored stop price, the entire remaining position is closed with a market order. This emulates the MetaTrader `OrderModify` stop-loss behaviour.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `UseCloseOneThird` | Close roughly one third of the position on the first checkpoint. | `true` |
| `LevelProfit1` | Profit in points that activates the first checkpoint. | `20` |
| `LevelProfit2` | Profit in points that activates the second checkpoint. | `35` |
| `LevelProfit3` | Profit in points that activates the third checkpoint. | `55` |
| `LevelMoving1` | Stop offset (points) applied at checkpoint 1. | `1` |
| `LevelMoving2` | Stop offset (points) applied at checkpoint 2. | `10` |
| `LevelMoving3` | Stop offset (points) applied at checkpoint 3. | `30` |
| `TrailingStop` | Distance (points) between price and trailing stop after checkpoint 3. | `30` |
| `TrailingStep` | Additional favourable movement (points) required before moving the trailing stop again. | `5` |
| `CandleType` | Candle series used to supply price updates. | `M5` timeframe |

All "point" values are multiplied by the instrument price step. If neither `PriceStep` nor `Step` is available, trailing is disabled because the conversion factor would be unknown.

## Additional Notes

- The strategy assumes real-time candles are subscribed via `SubscribeCandles`. Testing on historical data requires enabling candle emulation in StockSharp.
- The stop price is stored internally and no actual stop order is placed. If you need visible protective orders, you can add them manually outside of this strategy.
- MetaTrader specific features such as account-wide processing, comments, and sound alerts were intentionally omitted. Only the risk-management mechanics were ported.
- Because the closing order uses the entire current position size, make sure no other algorithm is manipulating the same security and portfolio simultaneously.
- Partial closing relies on the symbol's `VolumeStep`. Instruments without this metadata fallback to 0.1 lot rounding, mirroring the original EA behaviour.

## Conversion Differences vs. MetaTrader EA

- Orders are managed as a single net position instead of iterating through multiple tickets.
- Stop-loss adjustments are emulated by closing at market when the candle touches the calculated stop price.
- Sound notifications and chart comments were removed; use StockSharp logging if notifications are required.
- The `UseOneAccount` and `ShowComment` extern inputs do not exist because StockSharp strategies already operate on one instrument and logging facilities are available.

This detailed description should help reproduce the original trailing logic while integrating it into a StockSharp-based workflow.
