# Spread Data Collector Strategy

## Overview
The **Spread Data Collector Strategy** is a StockSharp port of the MetaTrader 5 utility "Spread data collector" (MQL entry 33314). The original expert advisor does not place orders; instead, it listens to the bid/ask stream and counts how many ticks fall inside predefined spread ranges. Whenever the trading year changes or the expert stops, it prints a statistical summary. This C# version reproduces the same behaviour using the high-level `SubscribeLevel1()` API and exposes the range thresholds as configurable parameters.

## Operation details
- The strategy subscribes to Level1 (bid/ask) updates of the main `Security` when it starts.
- Every time both bid and ask prices are available the strategy calculates the spread and converts it to price units by multiplying the configured point limits by `Security.PriceStep`.
- Six counters are maintained:
  1. Spread strictly below the first threshold.
  2. Spread in between the first and second thresholds.
  3. Spread in between the second and third thresholds.
  4. Spread in between the third and fourth thresholds.
  5. Spread in between the fourth and fifth thresholds.
  6. Spread above the fifth threshold.
- Year transitions are detected from the exchange timestamp (`Level1ChangeMessage.ServerTime`). When the year switches, the strategy prints the summary of the finished year and resets the counters.
- When the strategy stops, it prints the statistics of the current year before shutting down.

The port keeps the logging-only nature of the MQL utility, allowing traders to analyse how spreads behaved during different periods without sending any orders or manipulating positions.

## Parameters
All inputs are expressed in **points** (MetaTrader terminology). The actual price distance is calculated as `points × Security.PriceStep`.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `FirstBucketPoints` | 10 | Upper limit of the first spread bucket. Spreads strictly below this limit are counted in the first category. |
| `SecondBucketPoints` | 20 | Upper limit of the second spread bucket. Spreads in `[FirstBucketPoints, SecondBucketPoints)` are counted here. |
| `ThirdBucketPoints` | 30 | Upper limit of the third spread bucket. Spreads in `[SecondBucketPoints, ThirdBucketPoints)` increase this counter. |
| `FourthBucketPoints` | 40 | Upper limit of the fourth spread bucket. Spreads in `[ThirdBucketPoints, FourthBucketPoints)` are recorded here. |
| `FifthBucketPoints` | 50 | Upper limit of the fifth spread bucket. Spreads in `[FourthBucketPoints, FifthBucketPoints)` increase this counter. |

All thresholds must be strictly increasing. Attempting to start the strategy with invalid or non-positive `Security.PriceStep` values results in a runtime exception, which protects the user from inconsistent statistics.

## Logs and outputs
The statistics are printed through `AddInfoLog` in the following format:

```
Year=2024 Spread<=10pts=15342 Spread_10_20pts=2841 Spread_20_30pts=912 ... Spread>50pts=37
```

This output mirrors the `Print` statements of the MetaTrader expert, making it easy to compare both environments. Use the StockSharp log viewer or redirect logs to a file for further analysis.

## Usage checklist
1. Assign the target instrument to `Strategy.Security` and ensure that its `PriceStep` matches the MetaTrader point size (for most Forex symbols this equals 0.0001).
2. Adjust the bucket thresholds if you need different spread ranges. Keep the values strictly ascending.
3. Start the strategy and let it run. No orders will be sent.
4. Review the yearly logs to understand spread behaviour across sessions.

The strategy is intentionally lightweight and safe to run alongside live trading systems. It helps desks build historical spread distributions, validate liquidity assumptions, and monitor broker conditions over long periods.
