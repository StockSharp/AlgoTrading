# CCI Return Pending Orders Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades the return of CCI from its extreme zones through short-lived limit orders. The executable price comes from the finished candle close rather than a Level 1 bid or ask, so the lifecycle remains reproducible when quote fields are unavailable.

![schema](schema.svg)

## Strategy Overview

- Hourly candles feed CCI(30). Previous value distinguishes a return above -100 or below +100 from continued residence in an extreme zone.
- Position direction, a four-candle post-fill cooldown, and a global pending-order latch gate both sides.
- Order registering posts one limit order at the candle close with price shrinking disabled; only one pending order may be active.
- N values is armed by the registered Order and counts incoming candles. After four, Order cancellation removes an unfilled order and releases the latch.

## Entry and Exit Rules

- **Long entry**: Previous CCI is at or below -100, current CCI is above -100, Position is not long, the cooldown has elapsed, and no order is pending. A buy limit is posted at the close.
- **Short entry**: Previous CCI is at or above +100, current CCI is below +100, Position is not short, the cooldown has elapsed, and no order is pending. A sell limit is posted at the close.
- **Exit**: There is no fixed stop or target. An opposite CCI return can place a counter-order that nets the position toward flat. Any limit order still unfilled after its lifetime is cancelled without changing Position.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 01:00:00 | Time frame used for CCI, cooldown counting, order prices, and order lifetime. |
| CCI Length | 30 | Number of values in CommodityChannelIndex. |
| CCI Level | 100 | Symmetric overbought and oversold boundary used as +Level and -Level. |
| Signal Cooldown, candles | 4 | Finished candles required after the latest fill before a new entry may be submitted. |
| Order Volume | 1 | Quantity of each pending limit order. |
| Pending Lifetime, candles | 4 | Maximum number of finished candles an unfilled order may remain active. |

## Diagram Details

- The folder keeps its gallery position name, but no Level 1 field is connected: close is the explicit, replayable pending price.
- The pending-state flag is set by an actual registered Order and cleared by the order's Finished event, covering fills, cancellation, and failure.
- Cooldown and pending lifetime are separate parameters: one measures time after a fill, the other limits an unfilled order.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
