# Exp Extremum Strategy

This strategy trades reversals detected by comparing price extremes over a lookback window. It observes whether the current candle pushes the price beyond previous highs or lows and reacts when the sign of this comparison changes.

## How It Works

1. For each finished candle the strategy finds:
   - The lowest high over the last *N* bars.
   - The highest low over the last *N* bars.
2. Differences between the current high/low and these levels are summed.
3. A positive sum indicates upward pressure, a negative sum indicates downward pressure.
4. When the sign two bars ago opposes the sign one bar ago, a reversal signal appears:
   - Up then Down &rarr; open a long position.
   - Down then Up &rarr; open a short position.
5. Optional permissions allow disabling opening or closing of long/short positions independently.

## Parameters

- `Length` – indicator period for extreme calculations.
- `CandleType` – timeframe of incoming candles.
- `BuyPosOpen` / `SellPosOpen` – permissions to open long or short positions.
- `BuyPosClose` / `SellPosClose` – permissions to close long or short positions.

## Notes

The strategy uses the high-level API with candle subscriptions and built-in `Highest`/`Lowest` indicators. Positions are opened with market orders and closed via `ClosePosition()` when the opposite signal appears.
