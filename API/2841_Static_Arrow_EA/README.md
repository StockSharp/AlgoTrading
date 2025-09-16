# Static Arrow EA
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The strategy is a direct StockSharp port of the MetaTrader "Static Arrow" expert advisor. It is purely decorative: no orders are
sent and no trading rules are evaluated. When the strategy starts it subscribes to a candle series (weekly EURUSD by default),
creates a chart area for visualization, and prepares a row of arrow markers that should remain locked in place regardless of
incoming market data.

Unlike the MetaTrader script, the StockSharp implementation anchors every arrow to the most recent finished candle. This allows
the markers to keep their position even when the chart is scrolled or when new bars appear. The behavior is intentionally simple
so that the example can be used as a reference for plotting static annotations from a strategy without touching lower level
chart APIs.

## How it works
1. The strategy validates that the selected `CandleType` provides a time frame (only time-based candles are supported).
2. After subscribing to the candle stream it waits for the first finished bar. Its open time and close price become the anchor
   point for all markers.
3. Forty timestamps are generated (by default) by stepping backwards through the candle interval. Each timestamp is paired with
   the same anchor price plus an optional `PriceOffset`.
4. On every completed candle the cached coordinates are redrawn via `DrawText`, ensuring the arrows stay visible and stationary.
5. Because the coordinates are cached, the strategy does not need timers or tick processing—the chart annotations persist until
   the strategy stops or is reset.

## Parameters
- `CandleType` *(default: weekly TimeFrame)* – time frame of the candle subscription used for anchoring the markers.
- `ArrowCount` *(default: 40)* – number of arrow glyphs to display. The value is optimizable and can be reduced for
  performance or increased to stretch the row further into the past.
- `PriceOffset` *(default: 0)* – additive offset applied to the anchor candle price. Use this to shift the arrows slightly above
  or below the candles so that the glyphs do not overlap with bars.
- `ArrowSymbol` *(default: "↓")* – the exact text string printed on the chart. Any Unicode character (for example "↑" or
  emojis) can be used to customize the appearance.

## Usage notes
- The strategy is intended for visual dashboards and does not open or close positions. Risk and money management settings have
  no effect.
- Because the coordinates are stored after the first bar, changing parameters while the strategy is running requires a reset to
  recompute the arrow locations.
- The default weekly candle type mirrors the original EA that forced the EURUSD weekly chart. You can select any other
  instrument and time frame if you want to decorate different charts.
- When plotting on very small time frames consider decreasing `ArrowCount` to avoid drawing arrows outside of the visible range.

## Differences from the original MetaTrader code
- Chart options such as auto-scroll and zoom are not modified. StockSharp users can control them directly from the terminal.
- Pixel-based coordinates are replaced by time/price pairs derived from the first finished candle, which makes the example work
  uniformly across terminals and screen resolutions.
- The timer loop is unnecessary because StockSharp chart annotations remain persistent until removed; redrawing on every
  completed candle is sufficient to keep the markers visible.
