# Pure Price Action Fractal Strategy

## Overview
The **Pure Price Action Strategy** is a StockSharp port of the MetaTrader expert advisor "Pure Price Action" (MQL id 24291).
It combines breakout confirmation from Bill Williams fractals with a momentum filter computed on a higher timeframe and a long-term MACD trend filter.
The algorithm attempts to capture trend continuation trades immediately after the market retests the most recent fractal level.

## Trading Logic
1. **Signal candles** – Trading decisions are made on the user-selected timeframe (15 minutes by default).
2. **Fractal touch confirmation** – A trade is only allowed if the most recent finished candle closes within one price step of the latest confirmed fractal level (upper fractal for shorts, lower fractal for longs).
3. **Directional body pattern** – The absolute body size of the most recent candle must be smaller than the body of the previous candle, while the previous body must be larger than the candle before it. This mimics the momentum retracement filter from the original EA.
4. **Moving averages** – Two linear weighted moving averages (LWMA 6 and LWMA 85 by default) provide the baseline trend. Long trades require the fast LWMA to be above the slow LWMA; short trades require the opposite.
5. **Momentum filter** – A 14-period momentum indicator evaluated on a higher timeframe (H1 by default) must deviate from the equilibrium level (100) by at least the configured threshold during any of the last three momentum readings.
6. **MACD filter** – A MACD(12, 26, 9) indicator calculated on a higher timeframe (monthly by default) must show the main line above the signal line for longs and below for shorts.
7. **Position sizing** – The strategy uses the `Volume` property of the base `Strategy` class. If `Volume` is not set, it defaults to one contract/lot. The `MaxPosition` parameter limits the absolute position size.

## Position Management
- **Initial protection** – Optional fixed stop-loss and take-profit distances are specified in price steps and applied symmetrically to both sides.
- **Trailing stop** – When enabled, the strategy trails the highest/lowest price reached after the entry by the configured distance.
- **Break-even lock** – After the price travels by the trigger distance, the protective level is moved to entry ± offset to secure profits.
- **Manual exits** – The logic evaluates stop-loss, take-profit, trailing, and break-even levels on every finished candle and closes the entire position when any condition is met.

## Parameters
- `CandleType` – Main signal timeframe (default: 15-minute time frame).
- `MomentumCandleType` – Timeframe for the momentum indicator (default: 1-hour time frame).
- `MacdCandleType` – Timeframe for the MACD filter (default: 30-day time frame, emulating monthly candles).
- `FastPeriod` / `SlowPeriod` – Periods of the fast and slow LWMA.
- `MomentumPeriod` – Momentum indicator length.
- `MomentumThreshold` – Minimum absolute deviation of Momentum from 100.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD configuration.
- `StopLossPoints`, `TakeProfitPoints` – Risk protection distances in price steps.
- `TrailingStopPoints` – Trailing distance in price steps.
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – Break-even trigger and locked profit distances.
- `MaxPosition` – Maximum absolute position size handled by the strategy.
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – Toggles for risk-management blocks.

## Notes
- All comments inside the code are written in English, as required by the project guidelines.
- The strategy relies solely on finished candles; intra-bar signals are not processed.
- Multi-timeframe subscriptions are used to emulate the behaviour of the original expert advisor (M15 signal candles, H1 momentum, monthly MACD by default).
- No automatic tests are provided in this folder. The global repository test suite should remain untouched, as requested.
