# S7 Up Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Breakout system that looks for nearly equal highs or lows followed by a sharp price move.
When two consecutive lows are almost equal and price rises by `Span Price`, the bot enters long.
It enters short when two highs align and price drops by `Span Price`.
Positions are protected with optional take-profit, stop-loss, trailing stop and early exit features.

## Details

- **Entry Criteria:**
  - **Buy:** Difference between current and previous low is below `HL Divergence` and price is `Span Price` above the low.
  - **Sell:** Difference between current and previous high is below `HL Divergence` and price is `Span Price` below the high.
- **Long/Short:** Both.
- **Exit Criteria:**
  - Take profit or stop loss.
  - Trailing stop or zero trailing adjustment.
  - Early exit if price crosses previous high/low (`Exit At Extremum`) or nears reversal level (`Exit At Reversal`).
- **Stops:** Absolute take-profit and stop-loss with optional trailing.
- **Filters:** None.

## Parameters

- `Take Profit` – profit target in price units.
- `Stop Loss` – loss limit in price units, 0 for automatic extreme based stop.
- `HL Divergence` – maximum allowed difference between two consecutive highs or lows.
- `Span Price` – distance from extreme to price required for entry.
- `Max Trades` – maximum simultaneous trades.
- `Use Trailing Stop` – enable trailing stop mechanism.
- `Trail Stop` – trailing stop distance.
- `Zero Trailing` – move stop toward price once position is profitable.
- `Step Trailing` – minimal step to adjust zero trailing.
- `Exit At Extremum` – close if price crosses previous high/low.
- `Exit At Reversal` – close if price approaches opposite extreme.
- `Span To Revers` – distance from extreme to trigger reversal exit.
- `Candle Type` – timeframe used for analysis.
- `Order Volume` – quantity per trade.
