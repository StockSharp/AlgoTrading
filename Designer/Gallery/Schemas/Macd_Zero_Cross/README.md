# MACD Zero Line Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MACD is the distance between a fast and a slow exponential moving average, so the sign of the MACD line alone already says which average is on top. This diagram ignores the signal line and trades the moment the MACD line changes sign: from below zero to zero or above it buys, from above zero to below it sells.

![schema](schema.svg)

## Strategy Overview

- The MACD indicator is calculated with a fast period of 8, a slow period of 17 and a signal period of 9; only the MACD line takes part in the decisions, the signal line is computed but never read.
- A previous-value block keeps the MACD line of the preceding candle, so a sign change is recognised as a real cross and not as a state that simply lasts.
- The current position joins each condition, so a signal in the direction already held is dropped instead of enlarging the position.

## Entry and Exit Rules

- **Long entry**: The MACD line was below zero on the previous candle and is at or above zero on the current one, and the position is not long. The order buys the fixed volume, which opens a long from flat or closes an existing short.
- **Short entry**: The MACD line was at or above zero on the previous candle and is below zero on the current one, and the position is not short. The order sells the fixed volume, which opens a short from flat or closes an existing long.
- **Exit**: There is no separate exit block and no protective stop: every order carries the same volume, so the opposite zero cross brings the position back to flat rather than reversing it, and the next position is only opened on the following cross.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 8 | Period of the fast exponential moving average inside MACD. |
| Slow EMA length | 17 | Period of the slow exponential moving average inside MACD. |
| Signal EMA length | 9 | Smoothing period of the MACD signal line; it does not influence the trading decisions. |
| Volume | 1 | Order volume, in lots; the same value is used for opening and for closing. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the MACD indicator block, and a converter reads the MACD line out of the complex indicator value.
- A previous-value block shifts that line one candle back, and four comparison blocks test the previous and the current value against a shared zero constant.
- The same zero constant is compared against the position block, which gives the two guards Position <= 0 and Position >= 0.
- Each logical AND joins three conditions - the previous value, the current value and the position - and triggers a position modify block that sends a market order with the shared volume constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
