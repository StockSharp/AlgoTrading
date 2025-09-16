# Night Flat Trade Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Night Flat Trade Strategy reproduces the classic MQL5 expert advisor that looks for tight overnight ranges on EURUSD H1 candles. It focuses on the hour surrounding the trading day change, waiting for price to drift back toward the edges of a narrow consolidation channel and betting on a breakout continuation. The StockSharp version keeps the original ideas intact while relying on high-level candle subscriptions, indicator bindings and parameter objects for better configurability.

## Overview

- **Market & Timeframe**: Designed for EURUSD on the H1 timeframe, but any instrument with a clearly defined price step can be used.
- **Session Window**: Entries are allowed only during a two-hour window that starts at the configured `OpenHour` and ends at `OpenHour + 1` (exchange time).
- **Range Filter**: The high-low span of the last three completed candles must remain between `DiffMinPips` and `DiffMaxPips` (converted into price units).
- **Bias**: Long-only or short-only depending on where the latest close sits inside the qualifying range.

## Trading Logic

1. **Calculate Range Bounds**
   - The strategy binds to the built-in `Highest` and `Lowest` indicators (length = 3) to obtain the highest high and lowest low across the latest three candles.
   - The distance between those boundaries is the working range used for all subsequent checks.

2. **Entry Conditions**
   - **Long Setup**: During the active session, if the closing price is above the range low but still within the lower quarter (`lowest + range/4`), the strategy opens a long position with an initial protective stop at `lowest - range/3`.
   - **Short Setup**: Symmetrically, if the close is below the range high but still inside the upper quarter (`highest - range/4`), a short position is opened with a stop at `highest + range/3`.

3. **Exit Management**
   - **Stop-Loss**: Stops are simulated internally and trigger a market exit when the next candle breaches the stored threshold.
   - **Take-Profit**: When `TakeProfitPips > 0`, an additional fixed take-profit level (in pips) is created relative to the entry price.
   - **Trailing Stop**: When both `TrailingStopPips` and `TrailingStepPips` are positive, the stop is tightened only after price advances by `TrailingStop + TrailingStep` pips in favor of the trade. Subsequent adjustments require an extra `TrailingStepPips` of progress to mirror the original stepwise trailing behavior.

4. **Re-Entry Control**
   - The algorithm always waits for the current position to be completely closed before searching for a new signal, keeping the system flat between trades as in the reference expert advisor.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle series to subscribe to (defaults to H1). | 1-hour candles |
| `TakeProfitPips` | Optional take-profit distance in pips. | 50 |
| `TrailingStopPips` | Distance between price and trailing stop in pips (0 disables trailing). | 15 |
| `TrailingStepPips` | Extra pips required before each trailing stop update. | 5 |
| `DiffMinPips` | Minimum allowed three-candle range (pips). | 18 |
| `DiffMaxPips` | Maximum allowed three-candle range (pips). | 28 |
| `OpenHour` | Session start hour in exchange time (entries allowed until `OpenHour + 1`). | 0 |

## Indicators

- `Highest(Length = 3)` to monitor the recent range top.
- `Lowest(Length = 3)` to monitor the recent range bottom.

## Implementation Notes

- Pip conversion automatically adapts to instruments with 3 or 5 decimal places by multiplying the reported price step by 10, exactly like the original MQ5 implementation.
- Because StockSharp operates on completed candles in this sample, intra-candle entry conditions are approximated using the closing price. This keeps the logic deterministic while remaining faithful to the intent of the source code.
- All risk parameters are exposed through `StrategyParam<T>` objects, making them visible in the UI and ready for optimization or batch experiments.
