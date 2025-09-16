# Alliheik Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of the MetaTrader 4 expert advisor **alliheik.mq4**. The strategy combines a double-smoothed Heiken Ashi candle body with the forward-shifted Alligator "jaw" moving average. Entries occur when the Heiken Ashi buffers cross after the smoothing process. Exits rely on a jaw crossover filter, optional fixed targets, and a price-based trailing stop.

## Trading Logic

- **Heiken Ashi construction**
  - Smooth raw open, high, low, and close prices with `PreSmoothMethod` / `PreSmoothPeriod`.
  - Build classic Heiken Ashi candles from the smoothed prices.
  - Swap the high/low buffers depending on candle color (bullish keeps low/high order, bearish reverses them).
  - Apply a second smoothing pass (`PostSmoothMethod` / `PostSmoothPeriod`) to the conditional buffers. These are the values compared in the signal rules.
- **Signal definition**
  - **Long**: the current lower buffer is below the upper buffer while the previous bar had the opposite or equal relationship.
  - **Short**: the current lower buffer is above the upper buffer while the previous bar had the opposite or equal relationship.
- **Jaw filter and trailing**
  - The Alligator jaw is a moving average of `JawsPeriod` bars, shifted `JawsShift` bars forward and fed with `JawsPrice`.
  - `Close[6]` (six bars ago) must cross the jaw before the position can be closed automatically.
  - Once the difference between `Close[6]` and the jaw reaches eight points and reverses through the jaw, the position is closed.
  - If `TrailingStopPoints` is greater than zero, the stop price follows `Close[6]` once that candle is on the profitable side of the jaw.
- **Stops and targets**
  - `StopLossPoints` and `TakeProfitPoints` are optional fixed distances applied on entry.
  - Trailing logic overwrites the protective stop once it moves in favor of the trade.

## Default Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Time frame used for all calculations. |
| `JawsPeriod` | 144 | Length of the Alligator jaw moving average. |
| `JawsShift` | 8 | Forward displacement of the jaw (number of bars). |
| `JawsMethod` | Simple | Moving average type for the jaw (Simple, Exponential, Smoothed, Weighted). |
| `JawsPrice` | Close | Price component supplied to the jaw (Close/Open/High/Low/Median/Typical/Weighted). |
| `PreSmoothMethod` | Exponential | Moving average used to smooth raw OHLC values before computing Heiken Ashi. |
| `PreSmoothPeriod` | 21 | Period of the pre-smoothing averages. |
| `PostSmoothMethod` | Weighted | Moving average applied to the conditional Heiken Ashi buffers. |
| `PostSmoothPeriod` | 1 | Period of the post-smoothing averages (1 keeps the original buffers). |
| `StopLossPoints` | 0 | Fixed stop distance in points (0 disables). |
| `TrailingStopPoints` | 0 | Trailing stop distance based on `Close[6]` (0 disables). |
| `TakeProfitPoints` | 225 | Take-profit distance in points (0 disables). |
| `OrderVolume` | 0.1 | Lot size for entries. |

## Indicators Used

- Pre-smoothing MAs (four parallel series for open, high, low, close).
- Heiken Ashi reconstruction driven by the smoothed prices.
- Post-smoothing MAs of the conditional buffers that form the entry signal.
- Alligator jaw moving average with adjustable type, shift, and applied price.

## Entry and Exit Summary

- **Enter Long** when the smoothed lower buffer crosses below the upper buffer and the previous bar was not bullish (crossing condition described above).
- **Exit Long** when:
  - `Close[6]` falls back below the jaw after previously being above it and the distance reached ≥ 8 points; or
  - `TakeProfitPoints` target is reached; or
  - `StopLossPoints`/`TrailingStopPoints` stop is hit.
- **Enter Short** when the smoothed lower buffer crosses above the upper buffer and the previous bar was not bearish.
- **Exit Short** when:
  - `Close[6]` rises back above the jaw after previously being below it and the distance reached ≥ 8 points; or
  - `TakeProfitPoints` target is reached; or
  - `StopLossPoints`/`TrailingStopPoints` stop is hit.

## Conversion Notes

- The strategy enforces one trade per bar, mirroring the `isOrderAllowed()` check in the original EA.
- Protective stops and targets are simulated internally because StockSharp strategies cannot rely on broker-side MT4 orders.
- The jaw moving average stores historical values so that the forward shift replicates `iMA` behaviour with `ma_shift = JawsShift`.
- All computations use decimal arithmetic and indicator bindings consistent with StockSharp high-level API requirements.

## Risk and Usage

- Designed for both long and short trading on the same instrument.
- Works best on trending markets where the jaw shift and Heiken Ashi smoothing can highlight medium-term swings.
- Consider adjusting `TrailingStopPoints` and `TakeProfitPoints` to match instrument volatility.
- Always backtest and forward test on paper accounts before live deployment.
