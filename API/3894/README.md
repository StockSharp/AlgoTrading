# KSRobot 1.5 Strategy

## Overview
The **KSRobot 1.5 Strategy** is a C# conversion of the MetaTrader 4 expert advisor `KSRobot_1_5_h1_v1.mq4`. The StockSharp version keeps the original idea of trading Kijun-sen price breaks confirmed by a 20-period linear weighted moving average (LWMA) while enforcing a strict trading window and layered risk controls. All calculations are performed on 30-minute candles by default, but the timeframe can be changed through a parameter.

## Market data and indicators
- **Ichimoku** indicator with Tenkan/Kijun/Senkou Span B periods 6/12/24 by default.
- **Linear Weighted Moving Average (LWMA)** with length 20 to measure the slope and minimum distance filter.
- **Time-framed candles** defined by `CandleType` (defaults to M30) for signal generation.

## Trading logic
### Long workflow
1. A candle must interact with the Kijun line from below. Any of the following is sufficient: the candle opens below and closes above, the prior close was below while the new close is above, or the candle’s low pierces the level.
2. The latest Kijun value is flat or higher than two bars back, preventing trades against an immediate down move of the base line.
3. The LWMA is at least `MaFilterPips` (converted into price units) below Kijun. This reproduces the requirement that the moving average sits under the base line by a few pips.
4. The LWMA slope is positive (current LWMA greater than the previous bar).
5. The setup is stored as a pending long until the slope condition is satisfied; only one side can be pending at any given time, mimicking the `longcross`/`shortcross` flags from MQL.
6. When all criteria align and no net long exposure exists, a market buy order is submitted. The entry price cached by the strategy becomes the basis for stop, break-even and trailing management.

### Short workflow
Mirror conditions apply:
1. The candle interacts with Kijun from above (open above & close below, previous close above & current close below, or the high touches the level).
2. Kijun is flat or lower than two bars back.
3. The LWMA sits `MaFilterPips` above Kijun.
4. LWMA slope is negative compared with the previous bar.
5. Only one pending short is tracked and it is cleared once a long signal appears, just like the original expert.
6. When satisfied and the account is not already short, a market sell order is sent.

## Exit rules and risk control
- **Time window** – new trades are only considered while the candle open time is inside `[TradingStartHour, TradingEndHour)`, default 07:00–19:00 exchange time.
- **Initial stop-loss** – set `StopLossPips` below/above the entry price (converted via the instrument’s pip size). If zero, no initial stop is tracked.
- **Break-even move** – as soon as unrealized profit exceeds `BreakEvenPips`, the stop is moved to the entry price plus one pip for longs (minus one for shorts). This behaviour is controlled by `_breakEvenStep` to emulate the MT4 “move to BE+1” logic.
- **Trailing stop** – once price advances by `TrailingStopPips`, the stop trails at that distance in the favourable direction only.
- **Take-profit** – optional fixed target distance defined by `TakeProfitPips`. Set to zero to disable.
- **Slope exit** – if the LWMA turns against the trade before the stop has crossed the entry, the position is closed immediately. This captures the "MA turned wrong" exit from the MQL script.
- **Priority** – when both stop-loss and take-profit would be touched within the same candle, the stop-loss takes precedence to remain conservative with candle data.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Tenkan-sen length of the Ichimoku indicator. |
| `KijunPeriod` | 12 | Kijun-sen length (main trigger). |
| `SenkouSpanBPeriod` | 24 | Senkou Span B length. |
| `LwmaPeriod` | 20 | Period of the confirmation LWMA. |
| `MaFilterPips` | 6 | Minimum pip distance between LWMA and Kijun. |
| `StopLossPips` | 50 | Initial protective stop distance. |
| `BreakEvenPips` | 9 | Profit required before moving the stop to break-even. |
| `TrailingStopPips` | 10 | Trailing stop distance after price moves into profit. |
| `TakeProfitPips` | 120 | Optional fixed take-profit distance. |
| `TradingStartHour` | 7 | Inclusive hour to begin processing new trades. |
| `TradingEndHour` | 19 | Exclusive hour to halt new entries. |
| `CandleType` | 30-minute timeframe | Data type used for candle subscription. |

All pip-based parameters are converted into price units using `Security.PriceStep` (or `MinPriceStep`). Instruments quoted with three or five decimal digits receive an automatic ×10 multiplier to recreate standard FX pip sizing.

## Implementation notes
- The strategy binds both Ichimoku and LWMA indicators via `SubscribeCandles().BindEx(...)`, ensuring values come directly from the indicator pipeline without manual collections.
- Position management mirrors the MT4 expert: pending levels replace the `longcross`/`shortcross` flags and are cleared once a trade is triggered.
- Protective levels are cached after entry so that break-even and trailing decisions work with candle-level data even without individual order updates.
- `StartProtection` is invoked with zero distances because all protective actions are handled inside the strategy code, matching the bespoke MT4 logic.
- Only market orders are used. The original limit-vs-market selection relied on bid/ask ticks which are not available in candle-based backtests.

## Usage
1. Create the strategy instance, assign `Security`, `Portfolio`, `Volume`, and start it inside the StockSharp environment.
2. Optionally adjust pip-based parameters for the specific instrument. Optimized presets from the MQL comments (GBPUSD, EURUSD) can be reproduced by changing the defaults before running.
3. Keep an eye on the log output: entries, break-even moves, trailing adjustments and emergency exits are reported through `LogInfo` calls.
4. Attach the generated chart area (candles, Ichimoku, LWMA, own trades) in the designer or backtester to visualize the trade flow.

Only the C# version is provided. No Python folder is created according to the requirements.
