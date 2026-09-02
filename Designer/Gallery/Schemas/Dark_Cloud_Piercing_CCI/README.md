# Dark Cloud Cover / Piercing Line with CCI Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two classic two-candle reversal patterns choose the side, and the Commodity Channel Index decides whether the reversal is worth taking. A Piercing Line is bought only while CCI sits deep in negative territory, a Dark Cloud Cover is sold only while CCI is stretched to the upside. No signal ever closes a trade: a take profit and a stop loss placed at the entry do that.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern indicator blocks carry hand-written expressions that spell out the shape: the direction of the previous candle, the direction of the current one, where it opened and whether it closed past the middle of the previous body.
- The Commodity Channel Index over fourteen candles is the confirmation: the market has to already be stretched in the direction the pattern reverses, otherwise the shape is ignored.
- One entry level constant serves both sides, because a formula turns it into its own negative for the long comparison.
- Only a flat position may be entered, so a pattern that repeats on the next candle does not double the trade.

## Entry and Exit Rules

- **Long entry**: The previous candle is bearish, the current one is bullish, it opened below the previous close and closed above the middle of the previous body, CCI is below minus the entry level and the position is flat. The order buys one lot at market.
- **Short entry**: The previous candle is bullish, the current one is bearish, it opened above the previous close and closed below the middle of the previous body, CCI is above the entry level and the position is flat. The order sells one lot at market.
- **Exit**: Only the position protection block: a take profit two percent away from the entry price and a stop loss one percent away from it. The original strategy has no signal exit either, so nothing is missing here.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| CCI Length | 14 | Averaging length of the Commodity Channel Index. |
| Entry Level | 50 | How far CCI must be from zero for a pattern to count as confirmed; the long side uses the negative of this number. |
| Take Profit % | 2 | Take profit distance from the entry price, in percent. |
| Stop Loss % | 1 | Stop loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both pattern blocks, the Commodity Channel Index and the converter that reads the closing price for the protection block.
- A constant holds the entry level and a formula flips its sign, so a single optimizable number drives both CCI comparisons.
- Each logical AND joins a pattern, its CCI confirmation and the flat position check, and triggers a position modify block set to open only.
- Two things from the original are simplified: it also demands a true gap beyond the previous candle's low or high, which a continuously traded instrument practically never shows, and a pause of six candles between trades, for which no counter block exists. The open is therefore only required to be on the far side of the previous close, and every confirmed pattern is traded.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
