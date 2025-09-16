# TTM Trend Re-Entry Strategy

## Overview
This strategy recreates the logic of the MetaTrader expert advisor *Exp_ttm-trend_ReOpen*. It translates the TTM Trend indicator into the StockSharp framework, uses Heikin-Ashi smoothing to color candles, and trades when the color flips from bearish to bullish or vice versa. Each color change represents a regime shift in volatility compression/expansion, so the bot immediately closes any opposing exposure and opens a position in the new direction.

## Indicator Logic
The original indicator colors each bar according to both the Heikin-Ashi body and the classic OHLC candle:

- **Bright green (4)** – Heikin-Ashi close above its open and the standard candle closes higher than it opens.
- **Teal (3)** – Heikin-Ashi is bullish but the raw candle closes lower.
- **Deep pink (0)** – Heikin-Ashi is bearish and the raw candle closes lower.
- **Purple (1)** – Heikin-Ashi is bearish while the raw candle closes higher.
- **Gray (2)** – Neutral fallback if the trend cannot be determined.

To mimic the MetaTrader buffer smoothing, the indicator keeps a rolling window (`CompBars`) of previous Heikin-Ashi values. If the latest body remains inside the high/low envelope of any stored candle, the previous color is reused. This prevents whipsaws during small pullbacks, just like the source implementation.

## Trading Rules
1. Subscribe to the timeframe configured by `CandleType` and evaluate only finished candles (`SignalBar` selects how many closed bars to look back from the latest history point).
2. When a **bullish color** (values 1 or 4) appears and the previous signal was not bullish:
   - Close any short if `EnableShortExits` is active.
   - Open a long position (or flip from short to long) if `EnableLongEntries` is true.
3. When a **bearish color** (values 0 or 3) appears and the previous signal was not bearish:
   - Close any long if `EnableLongExits` is active.
   - Open a short position (or flip from long to short) if `EnableShortEntries` is true.
4. Each side can pyramid additional volume whenever price moves in the trade’s favor by at least `PriceStepPoints` (converted to price using the instrument’s `PriceStep`). The cumulative number of entries per direction is capped by `MaxPositions`.

## Pyramiding Behaviour
- `PriceStepPoints` mirrors the MetaTrader “PriceStep” input: once unrealized profit exceeds this distance from the average entry price, the bot adds the base `Volume` again.
- `MaxPositions` limits the total count of stacked entries, including the initial trade. Set it to `1` to disable re-entries entirely.

## Risk Management
`StopLossPoints` and `TakeProfitPoints` are measured in instrument points, just like in the original EA. They are transformed into absolute price distances via `Security.PriceStep` and applied through `StartProtection`. Set either parameter to zero to disable the respective protection leg.

## Parameters
- `CandleType` – timeframe used for the TTM Trend calculation (default: 4-hour candles).
- `CompBars` – number of historical Heikin-Ashi candles kept for color smoothing (default: 6).
- `SignalBar` – number of bars back from the latest finished candle to evaluate (default: 1 → last closed bar).
- `PriceStepPoints` – minimum favorable move, in points, required before pyramiding (default: 300).
- `MaxPositions` – maximum number of cumulative entries per direction (default: 10).
- `EnableLongEntries` / `EnableShortEntries` – toggle long/short openings on color flips.
- `EnableLongExits` / `EnableShortExits` – toggle forced exits when the opposite color appears.
- `StopLossPoints` – protective stop distance in points (default: 1000).
- `TakeProfitPoints` – profit target distance in points (default: 2000).

## Usage Notes
- The TTM Trend color logic is sensitive to the chosen timeframe; the original system used the H4 chart, but any `CandleType` can be supplied.
- Because the indicator works with Heikin-Ashi bodies, sudden gaps may not trigger a color flip immediately—wait for the next finished candle to confirm.
- Set `PriceStepPoints` to zero if you want a single-entry system that never pyramids.
