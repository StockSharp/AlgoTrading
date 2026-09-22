# RSI Momentum Limit Session Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines RSI(14), Momentum(14), a full-day Working time filter, and managed pending limits. An oversold weak-momentum setup places a buy below the candle open; an overbought strong-momentum setup places a sell above it, with explicit cancellation when either signal expires.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed RSI, Momentum, the candle-open entry price, and the close used to evaluate protection.
- Working time admits candles from 00:00 through 23:59, matching the source's effectively full-day session while keeping the session block visible and configurable.
- RSI below 30, Momentum below 1, and Position <= 0 arm a buy; RSI above 70, Momentum above 1, and Position >= 0 arm a sell.
- One-shot flags keep at most one order per valid signal episode. A stale own order or an opposing order is cancelled explicitly.
- A filled limit is managed by absolute take-profit 35 and stop-loss 8, with the finished close connected to the protection price input.

## Entry and Exit Rules

- **Long entry**: Inside the session, RSI < 30, Momentum < 1, and Position <= 0 register a one-unit buy limit at candle OpenPrice − 25. Any working sell is cancelled first.
- **Short entry**: Inside the session, RSI > 70, Momentum > 1, and Position >= 0 register a one-unit sell limit at candle OpenPrice + 25. Any working buy is cancelled first.
- **Exit**: Position protection closes a filled entry at an absolute gain of 35 or loss of 8 price units. A pending buy is cancelled when its RSI, Momentum, or position condition becomes invalid; the sell side is symmetric.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 00:05:00 | Finished-candle interval; five minutes is the replay adaptation, while C# defaults to fifteen minutes. |
| RSI Period | 14 | Number of values used by RelativeStrengthIndex. |
| Momentum Period | 14 | Number of values used by Momentum. |
| Session Start | 00:00:00 | Beginning of the Working time window. |
| Session End | 23:59:00 | End of the Working time window; 23:59 keeps the source's full-day behavior. |
| RSI Buy Threshold | 30 | RSI must be below this value for a buy setup. |
| RSI Sell Threshold | 70 | RSI must be above this value for a sell setup. |
| Momentum Threshold | 1 | Momentum must be below this value for buys and above it for sells. |
| Limit Offset, price units | 25 | Absolute distance subtracted from or added to candle OpenPrice in the replay diagram. |
| Order Volume | 1 | Volume of each pending limit order. |
| Take Profit, price units | 35 | Absolute favorable distance from the entry fill. |
| Stop Loss, price units | 8 | Absolute adverse distance from the entry fill. |

## Diagram Details

- The C# default candle type is 15 minutes. The diagram uses five-minute replay candles to produce enough observable signal and order cycles; both indicator periods remain 14, so this is an explicit sampling adaptation.
- The source offsets orders by 5 × PriceStep. The replay security's step is not consumed by a diagram block, so the diagram uses 25 absolute price units; this is a gallery execution setting, not the source default.
- Entry limits are calculated from OpenPrice exactly as in ProcessCandle. ClosePrice is separate and only supplies live price updates to Position protection.
- The source protection distances are 35 × PriceStep and 8 × PriceStep. The diagram preserves their numeric values as absolute price units rather than incorrectly presenting them as percentages.
- The one-shot flags model the source's active-order checks. They reset when RSI, Momentum, or the side-specific position condition invalidates the setup, allowing a later valid episode to register a new order.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
