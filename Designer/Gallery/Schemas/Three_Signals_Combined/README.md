# Combined MACD and Averaging Entry Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines a fresh EMA-difference crossover and a price-based averaging event into one long-entry stream. Every purchase is one unit, no more than five entries belong to a position cycle, and a two-percent recovery above the latest fill closes all counted units.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed formed EMA(12) and EMA(26) values and the candle close.
- Formula `Fast EMA - Slow EMA` creates the MACD line used by the diagram; Crossing detects its upward passage through zero.
- An upward zero crossing opens the first long only while Position is not positive and the entry counter is zero.
- While long, a close at least five percent below the latest entry fill adds one unit if fewer than five entries are counted.
- Combination merges exactly those two Boolean entry events into the trigger of one market Buy block.
- A close at least two percent above the latest entry fill sells the counted position size. The fill counter is then reset for the next cycle.

## Entry and Exit Rules

- **Initial long entry**: EMA(12) minus EMA(26) crosses zero upward, Position is less than or equal to zero, and Entries in Current Long equals zero. Buy one unit at market.
- **Averaging entry**: Position is positive, the counter is below Maximum Entries, and the finished candle closes at or below `Latest Entry Fill × (1 - Averaging Drop / 100)`. Buy one additional unit at market.
- **Exit**: While Position is positive, a finished candle closes at or above `Latest Entry Fill × (1 + Take Profit / 100)`. Sell the complete counted quantity at market and reset the counter.
- **Scope**: The diagram is long-only and manages the units opened by its own entry stream. It has no short entry and no stop-loss.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles Series | 00:05:00 | Finished five-minute candles used for every decision. |
| Fast EMA Length | 12 | Length of the fast exponential moving average. |
| Fast EMA Source | Not set | No alternate indicator input field is selected. |
| Slow EMA Length | 26 | Length of the slow exponential moving average. |
| Slow EMA Source | Not set | No alternate indicator input field is selected. |
| Maximum Entries | 5 | Maximum number of one-unit purchases in one long cycle. |
| Averaging Drop | 5 | Percentage decline from the latest entry fill required for another purchase. |
| Take Profit | 2 | Percentage rise from the latest entry fill required for the full exit. |
| Entry Volume | 1 | Fixed market volume of every initial or averaging purchase. |

## Diagram Details

- Candles emits finished values only and can build the five-minute series from smaller stored candles.
- Both EMA blocks emit only formed values. The first actionable MACD difference therefore appears after the slow EMA warm-up.
- Formula `a - b` receives the fast and slow EMA values, and Crossing compares that result with the zero Variable.
- Position supplies direction checks, while the explicit Entries in Current Long Variable enforces the zero-entry requirement and the five-entry cap.
- Each Buy fill supplies its average order price to Latest Entry Fill. Because every entry order is filled once, this is the execution price used by both percentage levels.
- A delayed counter path adds the filled order volume after each Buy. The full-exit fill sends zero back to the same counter before the next candle decision.
- Combination is Boolean and has two connected inputs: Fresh MACD Entry and Averaging Entry Below Step Five. Entry Volume is published after both branches so the current Boolean result is consumed.
- The chart receives candles, both EMA lines, the MACD line, latest entry price, averaging level, take-profit level, and all entry and exit fills.

## Usage

Import `Three_Signals_Combined.json` into Designer, provide enough history for EMA(26) to form, and test the five-minute setup for the selected instrument. Review the five-entry exposure and the absence of a stop-loss before using the diagram in live trading.
