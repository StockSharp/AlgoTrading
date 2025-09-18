# New Bar Strategy

## Overview

This sample demonstrates how to detect new bar events in StockSharp using the high-level candle subscription API. It is a conversion of the MQL expert advisor `NewBar.mq5`, which shows how to react when a fresh candle appears on the chart.

The strategy does not open real trades. Instead, it logs informative messages for the following situations:

- The very first update received after the strategy starts (equivalent to the MQL condition where `dtBarPrevious == WRONG_VALUE`).
- The first tick of each subsequent bar.
- Additional ticks that arrive while the current bar is still forming.
- The moment when a bar is closed.

## Core Logic

1. Subscribe to the configured candle series via `SubscribeCandles` and bind the `ProcessCandle` handler.
2. Track the current bar open time. When the open time changes, a new bar has started.
3. On the very first observation, call `HandleFirstObservation` to mimic the MQL example where the advisor is attached mid-bar.
4. For each new bar, call `HandleNewBar`, allowing custom logic such as generating orders or signals.
5. While the bar is active, invoke `HandleSameBarTick` to perform per-tick processing if required.
6. When `CandleStates.Finished` is observed, execute `HandleBarClosed`.

All helper methods contain English comments so the template can be easily extended with custom behaviour.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle series used for detecting new bar events. | 1-minute time frame |

## Extending the Template

- Place entry logic inside `HandleNewBar` to react exactly once when a bar starts.
- Add intrabar checks or risk management inside `HandleSameBarTick`.
- Close or trail positions when `HandleBarClosed` fires.

## Differences from the Original MQL Version

- Uses StockSharp's high-level candle subscription instead of manual time polling with `iTime`.
- Provides explicit helper methods that mirror the original comment blocks, making it clear where to plug in strategy-specific rules.
- Leverages structured logging through the built-in `Log` helper rather than inline comments.
