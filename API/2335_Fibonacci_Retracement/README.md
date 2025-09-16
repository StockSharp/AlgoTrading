# Fibonacci Retracement Strategy

This strategy trades Fibonacci retracement breakouts derived from ZigZag pivots.

## Idea

1. Detect swing highs and lows using a ZigZag approach.
2. Build Fibonacci retracement levels (23.6%, 38.2%, 61.8%, 76.4%) between the last two pivots.
3. In an uptrend the strategy buys when price closes above any Fibonacci level.
4. In a downtrend the strategy sells when price closes below any Fibonacci level.
5. Every order is protected with a fixed stop-loss and a take-profit based on the swing range.
6. After a position is closed the strategy waits a number of bars before trading again.

## Parameters

- `ZigzagDepth` – depth used to search for new pivots.
- `SafetyBuffer` – distance in points that price must move beyond the level.
- `TrendPrecision` – minimal difference between pivots to detect trend direction.
- `CloseBarPause` – number of bars to wait after closing a trade.
- `TakeProfitFactor` – fraction of the swing range used as take-profit extension.
- `StopLossPoints` – stop-loss distance from the entry price in points.
- `CandleType` – candle type used for calculations.

## Notes

This file contains only the C# implementation. A Python version is not yet provided.
