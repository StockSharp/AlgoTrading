# Kijun Sen Robot Strategy

## Overview
The **Kijun Sen Robot Strategy** is a direct conversion of the MetaTrader 5 expert advisor "Kijun Sen Robot" into the StockSharp high-level strategy API. It operates on 30-minute candles by default and focuses on Ichimoku Kijun-sen price crosses confirmed by a 20-period linear weighted moving average (LWMA). The strategy keeps the original expert's idea of trading only during the most active hours, enforcing position protection with dynamic stop, break-even and trailing logic.

## Indicators and data
- **Ichimoku** with Tenkan, Kijun and Senkou Span B configured to 6/12/24 periods.
- **Linear Weighted Moving Average (LWMA)** over 20 bars for slope confirmation and distance filtering.
- **30-minute candles** (default) for signal generation. Any other timeframe can be selected through the `CandleType` parameter.

## Trading logic
### Long entry
1. Candle trades through the Kijun line from below. The candle must either open below the line, close above it, or touch it intrabar while the previous close was also below.
2. Current Kijun is flat or rising compared to two bars back.
3. The LWMA is at least `MaFilterPips` (converted into price units) below the Kijun level, keeping the base line above the moving average.
4. LWMA slope is positive (current LWMA greater than the previous value).
5. Trading time is within `[TradingStartHour, TradingEndHour)`, default 07:00–19:00 exchange time.

When all conditions are satisfied and the strategy is not already net long, a market buy order is sent (any existing short is covered first). The entry price is the candle close.

### Short entry
1. Candle trades through the Kijun line from above (mirror of the long logic).
2. Kijun is flat or falling relative to two bars back.
3. The LWMA is at least `MaFilterPips` above the Kijun level.
4. LWMA slope is negative (current LWMA lower than the previous value).
5. Entry occurs only inside the allowed trading window.

A market sell order is placed (existing long exposure is closed before opening a short).

### Position management and exits
- **Initial stop-loss** – placed at `StopLossPips` below/above the entry price (converted to price units via the instrument price step). This reproduces the protective stop from the MQL version.
- **Break-even move** – once unrealized profit exceeds `BreakEvenPips`, the stop is moved to the entry price plus one pip (long) or minus one pip (short). The threshold is measured using the same pip conversion logic.
- **Trailing stop** – after the price advances by `TrailingStopPips`, the stop follows the price at that distance, only in the favorable direction.
- **Fixed take-profit** – optional target defined by `TakeProfitPips`. Set to zero to disable.
- **Kijun slope exit** – if the LWMA turns against the trade before the stop moves beyond breakeven, the position is closed immediately, matching the emergency exit from the original expert.
- **Time filter** – new trades are ignored outside the configured window, but open trades continue to be managed until closed by the rules above.
- **Order handling** – the StockSharp strategy uses market orders exclusively; the complex limit-vs-market entry logic from the original EA is simplified because candle data is used instead of tick data.

If both stop-loss and take-profit levels would be breached within the same bar, the stop-loss takes precedence to remain conservative without intrabar information.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Ichimoku Tenkan-sen length. |
| `KijunPeriod` | 12 | Ichimoku Kijun-sen length. |
| `SenkouSpanBPeriod` | 24 | Ichimoku Senkou Span B length. |
| `LwmaPeriod` | 20 | Length of the LWMA confirmation filter. |
| `MaFilterPips` | 6 | Minimum LWMA-to-Kijun distance in pips. |
| `StopLossPips` | 50 | Initial protective stop distance. |
| `BreakEvenPips` | 9 | Profit needed to move stop to break-even. |
| `TrailingStopPips` | 10 | Distance for trailing stop movement. |
| `TakeProfitPips` | 120 | Optional fixed take-profit distance. |
| `TradingStartHour` | 7 | First allowed trading hour (inclusive). |
| `TradingEndHour` | 19 | Last allowed trading hour (exclusive). |
| `CandleType` | 30-minute time frame | Data type used for signal evaluation. |

All pip-based parameters are translated into price units using the instrument `PriceStep`. Instruments with 3 or 5 decimal digits automatically receive a factor of 10 to replicate classic FX pip sizing.

## Implementation notes
- The conversion keeps the strategy stateful variables (`longcross`, `shortcross` behavior) via `_pendingLongLevel` and `_pendingShortLevel`, ensuring that new positions require a fresh Kijun cross.
- Intrabar checks such as "last bid/ask" from the MT5 version are approximated with candle-level conditions (`Open`, `Close`, `High`, `Low`). This makes the logic deterministic for backtesting in StockSharp.
- Position protection uses `ClosePosition()` and manual stop tracking instead of MT5 order modifications. The break-even and trailing adjustments are executed once per finished candle.
- The helper method `ConvertPips` performs pip-to-price conversion using `Security.PriceStep` or `Security.MinPriceStep`, applying a 10× multiplier for 3 or 5 decimal tick sizes to emulate the MT5 `digits_adjust` rule.
- Because the strategy is tied to the high-level API, indicators are bound via `SubscribeCandles().BindEx(...)`, and chart drawings are configured automatically (candles, Ichimoku, LWMA, own trades).

## Usage guidelines
1. Attach the strategy to a security that supports 30-minute candles (or set a different `CandleType`).
2. Configure `Volume` on the strategy instance to the desired order size before starting.
3. Optionally adjust pip-based parameters to reflect the instrument volatility or to reproduce optimized settings for specific currency pairs.
4. Run in the high-level backtester or live environment; the strategy will enforce the same trading window, stop and trailing rules as the original expert.
5. Monitor the log or chart to see break-even and trailing updates. All comments in the code are in English for clarity as requested.

The Python version is intentionally omitted; only the C# implementation is provided in this folder.
