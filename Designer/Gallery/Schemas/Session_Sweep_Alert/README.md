# Previous-Day Session Sweep Alert Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades failed breaks of the previous UTC day's range. It freezes that day's high and low at midnight, waits for a fifteen-minute candle to sweep a boundary and close back inside, enters against the sweep, and writes one log message for every accepted signal candle.

![schema](schema.svg)

## Strategy Overview

- Finished fifteen-minute candles provide the high, low, and close used by every decision.
- Highest(96) and Lowest(96) cover one full day. Previous value blocks with shift 1 exclude the new midnight candle before the range is captured.
- The captured high and low remain fixed from 00:00 UTC through the rest of that calendar day. Entry evaluation starts at 00:15 UTC.
- A failed break above the held high produces a short setup; a failed break below the held low produces a long setup.
- Entries are allowed only from a flat position and use one-unit market orders. If both sweep conditions occur on one candle, the high sweep has priority.
- Every filled entry receives a fixed 1% take-profit and 1% stop-loss, activated as market exits from finished-candle closes.

## Entry and Exit Rules

- **Long entry**: From 00:15 through 23:59:59 UTC, require the candle low to be below the held previous-day low, its close to be above that level, no simultaneous high-sweep setup, and Position to equal zero. Buy one unit at market.
- **Short entry**: In the same window, require the candle high to be above the held previous-day high, its close to be below that level, and Position to equal zero. Sell one unit at market.
- **Alert**: Each accepted long or short signal passes once through a per-candle Flag. String Formatter includes the signal close in a message sent by Notification to the strategy log.
- **Exit**: Position protection closes the filled entry after a favourable move of 1% or an adverse move of 1%. Both protective exits are market orders.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:15:00 | Time frame of the finished candles used for range construction and decisions. |
| Highest Length | 96 | Number of fifteen-minute highs in one complete daily range. |
| Highest Source | Not set | No alternate indicator input field is selected. |
| Lowest Length | 96 | Number of fifteen-minute lows in one complete daily range. |
| Lowest Source | Not set | No alternate indicator input field is selected. |
| Volume | 1 | Size of each market entry order. |
| Take Profit | 1% | Favourable move from the entry fill that activates protection. |
| Stop Loss | 1% | Adverse move from the entry fill that activates protection. |
| Trailing Stop Loss | false | Keeps the stop boundary fixed. |
| Use Market Orders | true | Sends activated protective exits as market orders. |

## Diagram Details

- HighPrice and LowPrice converters feed the two 96-value rolling indicators; ClosePrice supplies return-inside tests, alert text, and protection checks.
- Each rolling result passes through a finished Previous value block with shift 1. During 00:00-00:14:59 UTC, capture Variables take the preceding value, and hold Variables publish it on every candle.
- Four comparisons detect a high pierce with a close below the held high or a low pierce with a close above the held low. Working time restricts both combinations to 00:15-23:59:59 UTC.
- The position value is sampled on each candle and compared with zero. The short gate combines a high sweep with the flat check; the long gate additionally requires the inverted high-sweep signal to preserve priority.
- The two accepted gates trigger Buy and Sell Modify position blocks and are merged for reporting. A Flag reset by every candle prevents duplicate reporting within one signal event without suppressing later signals that day.
- Entry fills arm the shared Position protection block. Its reference price is updated by the finished close, so intrabar touches that recover before closing are not observed.
- The first usable range requires 96 preceding fifteen-minute candles. Levels are refreshed only at the midnight snapshot and remain unchanged until the next UTC day.
- The chart shows candles, rolling and held daily boundaries, buy and sell fills, and all strategy fills including protective exits.

## Usage

Import the `.json` file into Designer, run it in the backtester with enough history to form the first daily range, and adjust the time frame, range lengths, protection distances, and volume for the instrument before live trading.
