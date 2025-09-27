# MACD Sample Trend Filter Strategy

This strategy is a direct port of the classic MetaTrader 5 **MACD Sample** expert advisor. It uses MACD crossovers filtered by an EMA trend detector. Orders are sized with the standard `Volume` property, while risk management relies on configurable pip thresholds for the MACD histogram, take profit and trailing stop.

## Core logic

- **Indicators**
  - `MovingAverageConvergenceDivergenceSignal` with periods *(12, 26, 9)* provides MACD and signal lines.
  - `ExponentialMovingAverage` with period *26* acts as the trend filter.
- **Entry rules**
  - **Long**: MACD is below zero, crosses above the signal line, has magnitude above the *MACD Open Level*, and the EMA is rising.
  - **Short**: MACD is above zero, crosses below the signal line, has magnitude above the *MACD Open Level*, and the EMA is falling.
- **Exit rules**
  - MACD crosses against the position with magnitude above the *MACD Close Level*.
  - Take profit reaches the configured pip distance from the entry price.
  - Trailing stop (if activated by profit > trailing distance) is hit.
- **Trailing stop mechanics**
  - Long positions activate the trailing stop once the high price exceeds the entry price by the trailing distance. The stop is then maintained at *high − trailing distance*.
  - Short positions activate the trailing stop once the low price moves below the entry price by the trailing distance. The stop is maintained at *low + trailing distance*.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastPeriod` | 12 | Fast EMA period inside MACD. |
| `SlowPeriod` | 26 | Slow EMA period inside MACD. |
| `SignalPeriod` | 9 | Signal EMA period inside MACD. |
| `TrendPeriod` | 26 | Length of the EMA trend filter. |
| `MacdOpenLevelPips` | 3 | Minimum MACD magnitude (in pips) required to open a trade. |
| `MacdCloseLevelPips` | 2 | Minimum MACD magnitude (in pips) required to close a trade on crossover. |
| `TakeProfitPips` | 50 | Take-profit distance expressed in pips. |
| `TrailingStopPips` | 30 | Trailing stop distance expressed in pips. Set to 0 to disable trailing. |
| `CandleType` | 15-minute time frame | Candle type used for calculations. |

### Pip conversion

The original expert used MetaTrader's pip normalization (multiplying by 10 for 3/5-digit symbols). The conversion follows the same idea by inspecting `Security.PriceStep`:

- If the price step has 3 or 5 decimal places, the pip size is `PriceStep * 10`.
- Otherwise, the pip size equals `PriceStep`.
- When the price step is unavailable, pip-based thresholds fall back to raw values.

## Behavioural notes

- Positions are closed before new signals are evaluated, mirroring the MT5 implementation.
- `LogInfo` statements report entries, exits, and trailing stop updates for easier debugging.
- Protective orders are not placed automatically; exits are managed inside `ProcessCandle` to imitate the EA's logic.
- Use `Volume` to define the base trade size. Reversals automatically offset the current exposure by adding `Math.Abs(Position)` to the order volume.

## Differences from the MQL5 version

- Processing occurs on finished candles instead of every tick. This avoids repeated signals while maintaining deterministic behaviour.
- Trailing stop and take profit checks use candle highs and lows to approximate bid/ask hits from the original EA.
- When `Security.PriceStep` is missing, pip parameters act as absolute price distances and should be tuned manually.

Adjust the pip thresholds and candle type to fit the traded instrument, especially when porting to markets with different tick sizes.
