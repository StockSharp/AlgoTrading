# 2pb Ideal MA Re-Open Strategy

## Overview
- Implements the MQL expert "Exp_2pbIdealMA_ReOpen" using the StockSharp high level API.
- Trades a contrarian crossover between a single ideal moving average and a triple-staged ideal moving average.
- Adds to winning positions when price advances by a configurable number of ticks and optionally closes positions on opposite signals.

## Indicators
- **2pb Ideal 1 MA** – single ideal moving average with two weighting periods. It reacts quickly and defines the short-term bias.
- **2pb Ideal 3 MA** – triple cascade of the same ideal filter (stages X, Y, Z). It reacts slower and represents the background trend.

## Trading logic
1. Subscribe to the selected candle series (default H4) and evaluate signals on finished candles only.
2. Store filter values `SignalBarShift` bars back (default 1). Use the pair of values at shifts `SignalBarShift` and `SignalBarShift + 1` to detect crossings.
3. **Long entry** – when the fast filter was above the slow filter two bars ago and fell below it one bar ago (bearish cross), open a long position if long trading is enabled and no position is open.
4. **Short entry** – when the fast filter was below the slow filter two bars ago and rose above it one bar ago (bullish cross), open a short position if short trading is enabled and no position is open.
5. **Re-entries** – while a position is profitable, add one more order of `PositionVolume` once price moves by `PriceStepTicks * Security.PriceStep` in the trade direction. The number of add-ons per direction is limited by `MaxReEntries`.
6. **Exits** – if the opposite crossover appears and the respective exit flag is enabled, close the open position before considering new entries.
7. Apply optional stop loss and take profit using the configured tick distances.

## Parameters
- `CandleType` – timeframe of the working candle series.
- `PositionVolume` – base volume for entries and re-entries (also assigned to `Strategy.Volume`).
- `StopLossTicks` / `TakeProfitTicks` – protective distances expressed in ticks; converted to price using `Security.PriceStep`.
- `PriceStepTicks` – number of ticks required between successive re-entry orders.
- `MaxReEntries` – maximum number of add-on trades per direction.
- `EnableBuyEntries` / `EnableSellEntries` – allow opening long or short positions.
- `EnableBuyExits` / `EnableSellExits` – close existing positions when the opposite signal appears.
- `SignalBarShift` – number of bars back used to evaluate the crossover (mimics the original `SignalBar`).
- `Period1`, `Period2` – weights for the single ideal moving average.
- `PeriodX1`, `PeriodX2`, `PeriodY1`, `PeriodY2`, `PeriodZ1`, `PeriodZ2` – weights for each stage of the triple ideal moving average.

## Risk management
- Stop loss and take profit protections are activated through `StartProtection` if the corresponding tick distances are greater than zero.
- The strategy does not place new trades while an opposite position is still open, mirroring the MQL behaviour.

## Notes
- Works with any instrument that provides `Security.PriceStep`; default configuration targets H4 candles.
- No Python port is provided, matching the original request.
