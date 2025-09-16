# Morse Code Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The Morse Code Strategy replicates the original MetaTrader 5 expert that treats every finished candle as either a "dash" or a "dot". A bullish candle (close price greater than or equal to the open) is encoded as `1`, while a bearish candle (close price less than or equal to the open) is encoded as `0`. The strategy scans the latest sequence of completed candles and compares it to a user-selected binary mask. When the last candles match the configured sequence exactly, the strategy opens a position in the chosen direction and immediately attaches both a take-profit and a stop-loss order expressed in pips.

The implementation relies exclusively on high-level StockSharp APIs: candle subscriptions provide data, binding handles event delivery, and the built-in protection module manages exits. No custom collections or direct indicator value access are required, keeping the logic concise and robust.

## Pattern logic
- Candles are evaluated only after they are fully closed (`CandleStates.Finished`).
- Each candle becomes a binary digit:
  - `1` – the candle is bullish or neutral (`Close >= Open`).
  - `0` – the candle is bearish or neutral (`Close <= Open`). Doji candles match both digits, exactly as in the original expert.
- The mask is selected from the `MorsePatternMask` enumeration. It contains every binary sequence from length 1 to length 5 that appeared in the MT5 version (for example `000`, `1011`, `11111`).
- The strategy keeps a rolling window of the most recent candles. When the newest window matches the mask, the entry signal fires.

This behaviour mirrors the MT5 implementation that called `CopyRates` and compared each bar with the pattern string character by character.

## Trading workflow
1. Subscribe to the configured candle type and wait until enough bars are accumulated to cover the mask length.
2. For every completed candle:
   - Update the internal masks that classify the candle as bullish, bearish or neutral.
   - Ignore further checks until at least as many candles were processed as the mask requires.
   - If the latest candles match the selected mask exactly, check the desired direction.
   - Submit a market order in the direction of the signal (`BuyMarket` or `SellMarket`). When an opposite position exists, the strategy first closes it by increasing the order volume, reproducing the behaviour of the original expert adviser.
3. `StartProtection` immediately attaches a stop-loss and a take-profit offset expressed in price units. Protective orders are handled by the StockSharp engine using market exits to avoid missed fills.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 5-minute candles (`TimeSpan.FromMinutes(5).TimeFrame()`) | Data type used to build the Morse sequence. |
| `Pattern` | `_0` (`"0"`) | Binary mask to match against the most recent candles. Values come directly from `MorsePatternMask`. |
| `Direction` | `Sides.Buy` | Whether to open a long (`Buy`) or short (`Sell`) position when the pattern appears. |
| `TakeProfitPips` | `50` | Distance from entry to take profit in pips. The strategy automatically adapts to 3- and 5-decimal forex quotes by multiplying the price step by ten. |
| `StopLossPips` | `50` | Distance from entry to stop loss in pips, using the same pip calculation as above. |
| `Volume` (strategy property) | user-defined | Order size in lots/contracts, equivalent to `InpLot` in the MT5 expert. |

All parameters support the StockSharp parameter window, can be optimised, and can be changed before the strategy is started.

## Risk management
- `StartProtection` attaches both targets using price-based offsets derived from the pip settings. Exits are executed with market orders so that the behaviour matches the MT5 trade class that set stop-loss and take-profit values on position entry.
- Because the strategy does not pyramid, a new trade is ignored while an existing position in the same direction is open. When the pattern appears while holding the opposite direction, the volume is automatically increased to flip the position.
- Standard StockSharp logging reports every entry to the strategy journal.

## Usage notes
- The binary masks are intentionally short (up to five candles) to keep the logic faithful to the original idea. Consider combining multiple pattern masks through portfolio orchestration if a richer vocabulary is needed.
- Pip conversion relies on the instrument price step. For exotic symbols with non-standard increments you can adjust `TakeProfitPips` and `StopLossPips` manually.
- The strategy does not filter by time of day or volatility. You can wrap it inside a parent strategy that handles sessions or additional indicators if required.
- When testing, ensure the `Volume` property matches the expected lot size. The StockSharp tester will reuse the same protections and order flow as the live mode.

## Pattern reference
Examples of enumeration values:
- `_0` → `"0"` (single bearish candle)
- `_5` → `"11"` (two bullish candles in a row)
- `_20` → `"0110"` (bearish-bullish sequence forming a zig-zag)
- `_33` → `"00011"` (three bearish candles followed by two bullish ones)
- `_61` → `"11111"` (five consecutive bullish candles)

Any of the 62 masks can be selected from the parameter panel to reproduce the exact Morse code signature required by the trading plan.
