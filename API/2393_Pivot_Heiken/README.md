# Pivot Heiken Strategy

Strategy combining daily pivot points with Heikin-Ashi candles and an optional trailing stop. The daily pivot is calculated from the previous day's high, low and close. Heikin-Ashi smoothing filters price noise and highlights trend direction.

## Logic
- **Long entry**: Heikin-Ashi candle is bullish and the close is above the daily pivot.
- **Short entry**: Heikin-Ashi candle is bearish and the close is below the daily pivot.
- **Exit**: Position exits at stop loss, take profit, or trailing stop level.

## Parameters
- `CandleType` – working candle series.
- `StopLossPips` – stop loss distance in pips.
- `TakeProfitPips` – take profit distance in pips.
- `TrailingStopPips` – trailing stop distance in pips (0 disables trailing).

## Indicators
- Heikin-Ashi (calculated internally).
- Daily pivot point.

## Notes
- Uses high-level API with candle subscriptions and indicator binding.
- Suitable for both long and short trading.
