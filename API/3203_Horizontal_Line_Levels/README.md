[Русский](README_ru.md) | [中文](README_cn.md)

The **Horizontal Line Levels** strategy emulates the MetaTrader 5 expert advisor of the same name. It continuously rebuilds two price levels around the current quote and notifies the user once the market crosses them. The implementation relies on Level1 (bid/ask) market data, mimicking the original OnTick/OnTimer workflow without submitting any orders.

## Core Idea

1. Subscribe to Level1 data and cache the latest best bid and best ask prices.
2. Convert the MetaTrader point distance to the StockSharp price scale.
3. Offset the best ask upward and the best bid downward by the configured distance, creating two virtual horizontal lines.
4. Periodically check (via an internal timer) whether the bid or ask crosses those reference levels and log alerts in the strategy journal.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `TimerPeriodMinutes` | `1` | Minutes between two consecutive timer checks. Must stay positive. |
| `OffsetPoints` | `50` | Distance in MetaTrader points applied above the ask and below the bid when constructing the lines. |

## Behavior Details

- **Data subscription**: `GetWorkingSecurities` registers a Level1 stream so the strategy receives bid/ask updates even without candles.
- **Initialization**: The first time both best bid and best ask are available, `RecalculateLevels` stores the current upper and lower horizontal levels.
- **Timer**: Each timer tick recreates missing levels (if initialization happened before quotes were ready) and emits log messages once the market breaches either bound.
- **MetaTrader point translation**: The helper `EnsurePointSize` converts MetaTrader "points" into absolute price increments using the `Security.PriceStep`. The same technique is used in other converted strategies to maintain numeric compatibility.
- **No trading**: The strategy never sends orders; it only produces alerts through `AddInfoLog`. This matches the original expert which displayed pop-up alerts when the price touched either line.
- **Stop/Reset**: Stopping the strategy cancels the timer and clears all cached values so the next run starts from a clean state.

## Typical Usage

1. Attach the strategy to the desired instrument and set `TimerPeriodMinutes` and `OffsetPoints` in the Designer UI.
2. Start the strategy. Once a full quote snapshot arrives, a log entry such as `Horizontal levels updated. Upper: 1.12345, Lower: 1.12245.` confirms the calculated thresholds.
3. Watch the log window. When the ask rallies above the upper level (or the bid drops below the lower level) the strategy prints the corresponding alert message.
4. If you change the offset or restart the strategy, the levels are recomputed using the new parameters.

## Classification

- **Category**: Utilities / Alerts
- **Trading Direction**: None
- **Execution Style**: Event-driven monitoring
- **Data Requirements**: Level1 bid/ask
- **Complexity**: Basic
- **Recommended Timeframe**: Any (purely quote-driven)
- **Risk Management**: Not applicable (no positions opened)

This conversion keeps the alert-centric behavior of the MetaTrader original while leveraging StockSharp abstractions such as strategy timers and Level1 subscriptions.
