# MACD and Bollinger Middle Band Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two very common indicators split the work: MACD says which side of the market to be on, and the middle Bollinger band says when the price has strayed far enough from fair value to take that side cheaply. The outer bands are deliberately not used — the original strategy buys dips below the middle line, not breakouts of the envelope.

![schema](schema.svg)

## Strategy Overview

- The MACD line against its signal line is the only trend filter: above means long-only, at or below means short-only.
- The entry price has to be a tenth of a percent away from the middle Bollinger band, on the side the trend is not: dips are bought in an up-trend, spikes are sold in a down-trend.
- The gap is a percentage of the band, not a fixed number of points, so the same diagram works on any instrument.
- Exits do not wait for the price at all: as soon as the MACD lines swap places, the position is closed.

## Entry and Exit Rules

- **Long entry**: The MACD line is above its signal line, the candle closes below the middle Bollinger band minus the gap, and the position is not long. The order buys one lot, which opens a long from flat or covers a short.
- **Short entry**: The MACD line is at or below its signal line, the candle closes above the middle Bollinger band plus the gap, and the position is not short. The order sells one lot, which opens a short from flat or closes a long.
- **Exit**: A long is closed as soon as the MACD line drops to or below its signal line, a short as soon as the MACD line rises above it; both exit blocks are in close-position mode, so they only act when a position exists.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MACD fast period | 12 | Length of the fast moving average inside MACD. |
| MACD slow period | 26 | Length of the slow moving average inside MACD. |
| MACD signal period | 9 | Length of the MACD signal line. |
| Bollinger period | 20 | Averaging length of BollingerBands; only its middle line is read. |
| Bollinger width | 2.0 | Standard-deviation multiplier of BollingerBands; it does not affect the rules, since the outer bands are unused. |
| Middle band gap | 0.001 | Distance from the middle band an entry price has to reach, as a share of the band. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One candle block feeds MACD, BollingerBands and a converter for the close; three more converters pull the MACD line, the signal line and the middle band out of the indicator values.
- A single gap constant and two formula blocks turn the middle band into a buy level and a sell level, so one exposed number moves both thresholds at once.
- Each entry is a logical AND of three flags: the MACD comparison, the band comparison and the position compared against a zero constant.
- The two exit blocks hang directly off the MACD comparisons and run in close-position mode; all four order blocks take their size from the same volume constant.
- Deliberate simplifications: the original also subscribes to an AverageTrueRange that it never uses, so no ATR block is drawn, and it pauses entries for 100 bars after a trade, which no block can express — this diagram re-enters as soon as the conditions come back.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
