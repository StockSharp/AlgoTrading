# Macd Pattern Trader All v0.01
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the "MacdPatternTraderAll v0.01" MetaTrader expert advisor. It runs six independent MACD-based entry patterns on the same candle stream, manages risk with adaptive stop-loss and take-profit levels, performs staged profit taking, and optionally applies a slow martingale sizing rule after losing cycles.

## Core Features

- **Six MACD setups** – every pattern uses its own fast/slow EMA periods and threshold levels (`Pattern1` … `Pattern6`). Patterns can be toggled on or off independently.
- **Dynamic risk levels** – stop-loss levels are derived from recent highs/lows with configurable offsets, while take-profit levels iterate over successive bar blocks to mirror the original MQL implementation.
- **Session filter** – the strategy trades only inside the configurable `StartTime` / `StopTime` window when `UseTimeFilter` is enabled.
- **Partial exits** – profitable positions are scaled out in two steps once EMA/SMA filters confirm momentum, following the original `ActivePosManager` logic.
- **Slow martingale** – when `UseMartingale` is true the next trade size doubles after a losing cycle and resets after any profitable cycle.

## Entry Logic by Pattern

1. **Pattern 1 (tag `Pattern1`)**
   - Arms a short after the MACD main line pushes above `Pattern1MaxThreshold` and then rolls over with a lower high sequence.
   - Arms a long after stretching below `Pattern1MinThreshold` and producing a higher low sequence.
2. **Pattern 2 (tag `Pattern2`)**
   - Counts oscillations around the zero line. Shorts are triggered when a positive swing fails near `Pattern2MinThreshold`. Longs appear when a negative swing fades near `Pattern2MaxThreshold`. The algorithm reproduces the original distance checks by comparing absolute MACD values (`valueMin2` / `valueCurr2`).
3. **Pattern 3 (tag `Pattern3`)**
   - Tracks up to three descending (or ascending) MACD tops to detect a "triple hook". Only when all intermediate thresholds (`Pattern3MaxThreshold`, `Pattern3MaxLowThreshold`, `Pattern3MinThreshold`, `Pattern3MinHighThreshold`) agree does it allow new positions.
4. **Pattern 4 (tag `Pattern4`)**
   - Watches for MACD spikes outside `Pattern4MaxThreshold` / `Pattern4MinThreshold` followed by failed attempts to make new extremes. An extra counter (`Pattern4AdditionalBars`) is preserved for compatibility.
5. **Pattern 5 (tag `Pattern5`)**
   - Implements the neutral-zone breakout used in the expert advisor. Shorts require a rebound from below `Pattern5MinThreshold` back inside the neutral zone and another failure. Longs follow the mirrored sequence around `Pattern5MaxThreshold`.
6. **Pattern 6 (tag `Pattern6`)**
   - Counts the number of consecutive bars above/below threshold levels. After spending more than `Pattern6TriggerBars` inside the overbought/oversold area and returning beneath/above the threshold, the strategy opens a trade unless `Pattern6MaxBars` blocked the signal.

Each pattern uses the helper methods `TryOpenLong` / `TryOpenShort`, guaranteeing that stops and targets are calculated before any order is issued.

## Risk and Trade Management

- **Stop-loss**: `CalculateStopPrice` scans the most recent `stopBars` finished candles (excluding the active one) and applies the configured point `offset`. Prices are adjusted for 3/5 decimal instruments just like in the MQL version.
- **Take-profit**: `CalculateTakeProfit` walks through consecutive blocks of `takeBars` candles until no new extreme is found, mimicking the nested `iLowest` / `iHighest` loop from the original code.
- **Partial exits**: `ManageActivePositions` closes one third of the position at `ProfitThreshold` profit when price confirms with `ema2`. A second half-sized exit fires when price reaches the combined `(sma3 + ema4) / 2` filter.
- **Hard exits**: `CheckRiskManagement` issues full market exits once the stored stop-loss or take-profit levels are touched.
- **Martingale control**: `OnOwnTradeReceived` accumulates realized PnL for the current flat-to-flat cycle. When the position returns to flat, `AdjustVolumeOnFlat` either resets the volume to `InitialVolume` after profits or doubles it after losses if `UseMartingale` is enabled.

## Parameters

All configuration knobs are exposed through `StrategyParam<T>` properties for optimization in StockSharp Designer.

- **General**: `CandleType`, `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`.
- **Patterns 1–6**: stop-loss/take-profit bar counts, offsets, MACD fast/slow periods, and threshold levels matching the external inputs from the MQL script.
- **Position manager**: EMA/SMA lengths (`EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4`) used in the partial exit filter.

All default values mirror the `extern` variables of `MacdPatternTraderAll v0.01`.

## Usage Notes

- The strategy expects a symbol with a valid `PriceStep` and `Decimals` to compute offsets correctly.
- Provide a candle series via `CandleType` (for example, `TimeSpan.FromMinutes(5).TimeFrame()`).
- When several patterns trigger simultaneously the strategy will open only one position because every entry call recalculates the combined desired volume and clears opposite stops.
- The staged exit logic works with aggregated positions, so partial closes occur even if multiple patterns share the same trade direction.

