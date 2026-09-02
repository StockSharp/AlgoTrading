# Ichimoku Tenkan/Kijun Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Ichimoku system is used here in full: the fast pair of lines gives the signal and the cloud decides whether that signal is allowed. Tenkan-sen crossing Kijun-sen is the trigger, and the position is only opened when the close sits on the same side of the Kumo cloud as the cross points.

![schema](schema.svg)

## Strategy Overview

- A single Ichimoku block builds all the lines, and four converters read Tenkan-sen, Kijun-sen, Senkou Span A and Senkou Span B out of its complex value.
- Two formula blocks fold the two Senkou lines into the top and the bottom of the cloud, so the close can be tested against the cloud with one comparison per side.
- Entries are only made from flat, which is checked twice over: by comparing the position against zero and by the open-position condition of the order block itself.
- Exits are separate blocks: either the opposite cross or a close that has fallen back through the cloud sends the position home, and the closing blocks take their size from the open position.
- The original ignores every signal for 500 candles after a fill, which also delays its exits; a bar counter cannot be built out of these blocks, so that pause is left out and the diagram trades more often than the original.

## Entry and Exit Rules

- **Long entry**: Tenkan-sen crosses above Kijun-sen, the close is above the top of the cloud and the position is flat. The order buys the fixed volume and opens the long.
- **Short entry**: Tenkan-sen crosses below Kijun-sen, the close is below the bottom of the cloud and the position is flat. The order sells the fixed volume and opens the short.
- **Exit**: A long is closed when Tenkan-sen crosses back below Kijun-sen or the close drops below the bottom of the cloud; a short is closed on the mirror image of that. The closing order is sized from the position, so the diagram returns to flat instead of reversing, and there is no stop loss or take profit, exactly as in the original.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Tenkan period | 9 | Period of Tenkan-sen, the midpoint of the highest high and the lowest low over that many candles. |
| Kijun period | 26 | Period of Kijun-sen, built the same way over a longer window. |
| Senkou Span B period | 52 | Period of Senkou Span B, the slower of the two cloud borders. |
| Volume | 1 | Order volume, in lots, used to open a position; the exits close whatever size is open. |
| Candles | 00:01:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Ichimoku indicator and a converter for the close price.
- Tenkan-sen and Kijun-sen meet in a crossing block whose output is the bullish cross; a logical NOT of it is the bearish cross.
- The two cloud comparisons are shared between the entries and the exits: above the cloud both opens a long and closes a short, below the cloud does the mirror image.
- Each entry runs through a logical AND with the flat check, while each exit runs through a logical OR, so either the cross or the cloud break is enough to trigger a close-position block.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
