# Forex Fraus Portfolio Strategy

This strategy trades a single instrument based on the **Williams %R** indicator with a long period. When the indicator leaves extreme zones, the strategy opens positions in the direction of the breakout.

## How it works

1. Calculate Williams %R over `WprPeriod` candles.
2. When the indicator drops below `BuyThreshold`, a long opportunity is prepared. Once it rises above the threshold, a market buy order is placed.
3. When the indicator rises above `SellThreshold`, a short opportunity is prepared. Once it falls below the threshold, a market sell order is placed.
4. Positions are allowed only during the time window between `StartHour` and `StopHour`.
5. Optional stop loss, take profit and trailing stop can be enabled through parameters.

## Parameters

- `WprPeriod` – Williams %R period.
- `BuyThreshold` – value to enable a long signal.
- `SellThreshold` – value to enable a short signal.
- `StartHour` / `StopHour` – trading session limits.
- `SlPoints` – stop loss in points. Disabled if 0.
- `TpPoints` – take profit in points. Disabled if 0.
- `UseTrailing` – enable trailing stop logic.
- `TrailingStop` – trailing distance in points.
- `TrailingStep` – step for trailing updates.
- `CandleType` – candle type to subscribe.

## Notes

The original MQL4 version traded multiple currency pairs and managed orders for each one. This C# port focuses on a single instrument and demonstrates the core idea using the high-level API of StockSharp.
