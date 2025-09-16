# High and Low Last 24 Hours
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy replicates the MetaTrader example *EA High and Low last 24 hours*. Instead of waiting for the user to press the **H** or **L** keys, it continuously scans the trailing lookback window (24 hours by default) and exposes the most recent highest and lowest prices.

## Overview

- Subscribes to the configured candle type and keeps a rolling 24-hour window.
- Uses the built-in `Highest` and `Lowest` indicators so that no custom buffers are required.
- Automatically recalculates the indicators when the window length parameter changes during a session.
- Logs the current extremes with timestamps that match the original script output.
- Draws horizontal price levels at the detected high and low, plus a vertical marker showing the start of the analysed window.
- Does not place any orders; it is strictly an analytical helper that mirrors the behaviour of the MQL script.

## Detailed Workflow

1. **Data subscription** – when the strategy starts it subscribes to candles produced from the selected `Candle Type`. The time frame is extracted from the `DataType.Arg` payload.
2. **Window sizing** – the length of the `Highest`/`Lowest` indicators is calculated as `ceil(WindowLength / timeFrame)`, ensuring the lookback always covers the requested duration even if the candles do not divide 24 hours evenly.
3. **Real-time updates** – every finished candle triggers recalculation of the sliding window. If the `Window Length` parameter is edited on the fly, the indicator lengths are updated immediately.
4. **Logging** – when either extreme changes, the strategy prints an info message that includes the window boundaries and the new price. This mirrors the `Comment()` output of the original MQL implementation.
5. **Chart visuals** – three persistent drawings are updated on each candle:
   - a horizontal line at the highest price across the window,
   - a horizontal line at the lowest price,
   - a vertical line at the beginning of the window, spanning from the low to the high.

## Parameters

### `Window Length`
Duration of the rolling window that is analysed (default: 24 hours). Any positive `TimeSpan` is accepted, enabling intraday or multi-day studies. Shorter windows reduce the indicator length, while longer ones broaden the search horizon.

### `Candle Type`
Specifies the candle data source for the calculation. By default the strategy uses 15-minute time-frame candles, but any time-based candle type can be selected. The time-frame value is also used to translate the window duration into the indicator length.

## Trading Rules

- **Entries/Exits**: none. The strategy never sends orders and therefore cannot open or close positions.
- **Risk Management**: not applicable.

## Practical Notes

- Because the original script responded to manual keystrokes, this conversion keeps the intent by refreshing continuously instead of waiting for user input.
- The log messages are emitted only when the extreme actually changes, preventing redundant noise in the message panel.
- The visual levels help to reproduce the horizontal/vertical lines that MetaTrader drew for the highest and lowest prices in the inspected period.
- The approach works with any instrument supported by StockSharp because it relies solely on standard candle subscriptions and built-in indicators.
