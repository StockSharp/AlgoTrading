# Open Time Two Strategy

## Overview
The **Open Time Two Strategy** automates a time-scheduled trading plan that can manage two independent sessions during the trading day. Each session can be configured with its own direction, risk parameters, and optional forced closing window. The conversion follows the original MetaTrader logic but relies on StockSharp high-level APIs, candles, and parameter objects for configuration and optimization.

## Trading Logic
1. **Session closing windows.** For each interval an optional closing window can be defined. When the candle time falls inside the window (start time plus the global duration), the strategy force-closes the corresponding interval and clears its state.
2. **Trailing stop maintenance.** If the trailing stop and step are positive, trailing logic monitors finished candles. Once price moves in favour of the position by at least `(TrailingStop + TrailingStep)` the stop is advanced by `TrailingStop`. Updates require the step distance to avoid noisy recalculation.
3. **Stop loss and take profit checks.** Each interval has independent stop-loss and take-profit distances measured in pips. On every finished candle the high/low prices are compared against these levels, closing the interval immediately when a level is breached.
4. **Day-of-week filter.** Trading proceeds only on the enabled weekdays. If the current candle belongs to a disabled day, no new trades are opened.
5. **Opening windows.** Each interval has an opening window with start and end times. The global duration value extends the window on the end side. When a window is active and the interval has no open position, the strategy opens a market order in the configured direction.
6. **Position synchronisation.** Active intervals contribute to a target net position. The strategy calls `BuyMarket` or `SellMarket` so the net position matches the sum of interval exposures. Each interval keeps its own entry price, stop/take levels, and trailing stop state.

## Parameter Reference
- **Close Window #1 / Close Window #2** – enable or disable the dedicated forced-closing windows for each interval.
- **Close Start #1 / Close Start #2** – local time-of-day when the closing window starts for each interval.
- **Trailing Stop / Trailing Step** – distances in pips used by the trailing logic. Both must be greater than zero to activate trailing.
- **Trade Monday … Trade Friday** – day-of-week filters. At least one day must remain enabled to allow trading.
- **Open Start #1 / Open End #1 / Open Start #2 / Open End #2** – opening window limits for each interval. The global duration extends the window beyond the end time.
- **Window Duration** – extra time span added to both opening and closing windows.
- **Direction #1 / Direction #2** – trade direction flags (`true` for long, `false` for short) for each interval.
- **Trade Volume** – market order volume for each interval. The strategy assumes identical volume for both intervals as in the original expert advisor.
- **Stop Loss #1 / Take Profit #1 / Stop Loss #2 / Take Profit #2** – distances in pips for stop loss and take profit levels per interval. A value of zero disables the corresponding level.
- **Candle Type** – candle series used to drive the strategy. All calculations, including time windows and risk checks, are executed when these candles finish.

## Risk Management Details
- Pip distances are converted to price units using the security price step. If the instrument uses three or five decimal places the step is multiplied by ten to replicate the MetaTrader pip definition.
- Trailing logic is shared by both intervals, while stop-loss and take-profit values remain independent.
- When the stop or trailing level triggers, the interval resets its state so that it can reopen inside the same window if time allows.

## Limitations & Notes
- StockSharp operates on a netting position model. If interval #1 and #2 are configured with opposite directions, the resulting net position will flatten rather than keep two hedged trades open simultaneously. Use a hedging-capable portfolio if true hedging is required.
- Decisions are based on the selected candle series. Using a large timeframe can delay reactions compared to the tick-based MetaTrader implementation.
- The strategy expects exchange and terminal clocks to be synchronised because time-of-day comparisons are local-time based.

## Usage Tips
- Configure candle type to match the time granularity used for the schedule (e.g., one minute for granular control).
- Combine the day filter and closing windows to avoid carrying positions over undesirable sessions.
- Optimise the parameters through the built-in `StrategyParam` objects – key fields already have `SetCanOptimize` enabled.
