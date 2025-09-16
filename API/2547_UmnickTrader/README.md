# UmnickTrader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive mean-reversion system converted from the original UmnickTrader MQL5 expert advisor. The strategy works with a single
position at a time, alternating between long and short bias depending on the outcome of the previous trade. It evaluates price
movement using the average of open, high, low and close prices and only takes action once that average has shifted by at least
the configured `StopBase` distance.

## Core Logic

- For every finished candle the average price `(O + H + L + C) / 4` is calculated.
- Signals are processed only when the absolute difference between the current average and the previously processed average is
greater than or equal to `StopBase`. This mimics the original EA behaviour of waiting for a sufficiently large move.
- When no position is open the strategy computes adaptive take-profit and stop-loss distances using two circular buffers that
store the most recent eight profit and loss excursions.
- After a profitable trade the maximum favourable excursion observed while the position was open is saved to the profit buffer
(minus a spread padding), while the loss buffer receives `StopBase + 7 * Spread`.
- After a losing trade the profit buffer is reset to `StopBase - 3 * Spread`, the loss buffer is updated with the recorded drawdown
plus a spread padding, and the trading direction is flipped so the next setup trades the opposite side.

## Trade Management

- The default distance for both the take-profit and stop-loss is `StopBase`. If the accumulated profit or loss buffer exceeds
`StopBase / 2`, their respective averages replace the default distance to widen or tighten the exit levels adaptively.
- Market orders are used for entries. The expected take-profit and stop-loss prices are stored and managed by the strategy itself,
so positions are closed when candle highs or lows touch the corresponding levels.
- While a position is active the highest favourable move and deepest drawdown are tracked using intrabar extremes. These statistics
feed the buffers when the trade closes.
- Only one position can exist at any moment. A new signal is ignored if the previous trade has not been completed.

## Parameters

- `StopBase` – base distance (in price units) required to treat a move as significant and the default TP/SL distance. Default:
  `0.017`.
- `TradeVolume` – volume for market orders. Default: `0.1`.
- `Spread` – spread compensation applied when updating the adaptive buffers. Default: `0.0005`.
- `CandleType` – candle subscription used to evaluate averages. Default: `TimeSpan.FromMinutes(5).TimeFrame()`.

## Classification & Filters

- **Direction**: Both (but never simultaneously).
- **Style**: Adaptive swing / counter-trend.
- **Indicators**: Price average, custom excursion buffers.
- **Stops**: Dynamic stop-loss and take-profit managed by the strategy.
- **Complexity**: Intermediate – combines stateful buffers with adaptive exit sizing.
- **Timeframe**: Configurable via `CandleType`.
- **Seasonality / News Filters**: Not used.
- **Risk Management**: Position size is fixed by `TradeVolume`; exit distances adapt based on recent performance.
