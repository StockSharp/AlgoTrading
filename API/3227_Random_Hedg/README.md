# Random Hedg Strategy

## Overview
The **Random Hedg Strategy** is a StockSharp high-level port of the MetaTrader expert advisor "Random Hedg". The original EA opens a market buy and market sell simultaneously, then manages both legs with a mixture of fixed stop loss, take profit, break-even and trailing logic. The conversion keeps that core behaviour while exposing every setting as a strategy parameter so the bot can be tuned or optimized directly inside StockSharp Designer.

## Trading Logic
1. **Initial hedge** – when the strategy is flat it immediately sends two market orders (buy and sell) using the same configurable volume. Both legs receive a stop loss and a take profit expressed in pips.
2. **Break-even guard** – after price moves in favour of a leg by the configured number of pips, the stop level is shifted to break even plus an optional offset (long positions) or break even minus the offset (short positions). This mirrors the "move to no loss" toggle from the EA.
3. **Trailing stop** – once profit exceeds the trailing distance, the stop follows price. For longs the stop trails the highest price minus the trailing distance; for shorts it trails the lowest price plus the distance.
4. **Protective exits** – every leg is closed when its take profit or stop loss is touched. Optionally the strategy can liquidate both legs if candle closes below the lower Bollinger Band, recreating the exit filter from the original code.
5. **Cycle restart** – once both legs are closed the strategy resets its internal trackers and waits for the next candle to open a new hedged pair.

## Parameters
- `HedgeVolume` – volume used to open both hedge legs (default 0.1 contracts).
- `StopLossPips` – distance of the protective stop loss (default 200 pips).
- `TakeProfitPips` – distance of the take profit (default 200 pips).
- `TrailingStopPips` – trailing step applied after a position becomes profitable (default 40 pips).
- `BreakEvenTriggerPips` – profit required before moving the stop to break even (default 10 pips).
- `BreakEvenOffsetPips` – additional profit locked in when the break-even move happens (default 5 pips).
- `EnableTrailing` – enables or disables trailing stop management.
- `EnableBreakEven` – enables or disables the break-even feature.
- `EnableExitStrategy` – enables the Bollinger Band-based liquidation filter.
- `BollingerPeriod` – period of the Bollinger Bands used for the optional exit (default 20 candles).
- `BollingerWidth` – width multiplier of the Bollinger Bands (default 2).
- `CandleType` – candle data series used to drive the logic (default 30-minute time frame).

## Implementation Notes
- The conversion uses the high-level `Strategy` API with candle subscriptions and the `BindEx` mechanism to calculate Bollinger Bands on the fly.
- Internal state tracks the entry price, volume and dynamic protective levels for each leg. This allows the C# version to mimic the money-management helpers from the original EA without relying on platform-specific order handles.
- Pending order volumes are tracked separately so that fills can be classified as entries or exits even when buy and sell trades happen back-to-back.
- The strategy expects a hedging-capable account because it keeps long and short exposure at the same time, just like the source expert advisor.
- Money-based trailing and percentage take-profit features from the MQL code are intentionally omitted. They depend on broker-specific balance data and were rarely used in practice; the StockSharp version focuses on the core price-action management.

## Files
- `CS/RandomHedgStrategy.cs` – main C# implementation with detailed English inline comments.
- `README.md` – this documentation (English).
- `README_ru.md` – Russian translation.
- `README_cn.md` – Simplified Chinese translation.
