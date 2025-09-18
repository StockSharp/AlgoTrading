# MA Price Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MA Price Cross strategy is a direct conversion of the MetaTrader 4 expert advisor "MA Price Cross" to the StockSharp high-level API. It waits for the selected moving average to cross the current price while trading is allowed within a configurable time window. When the crossing happens from below, the algorithm opens a long position; when the crossing happens from above, it opens a short position. Protective stop-loss and take-profit distances are defined in MetaTrader points and automatically translated to absolute price offsets using the instrument's `PriceStep`.

Unlike the original MQL implementation, which reacts on every tick, the StockSharp version works with finished candles and uses the `SubscribeCandles` high-level subscription. This ensures that trading decisions are executed once per bar and remain compatible with the indicator binding pipeline. The moving average can be configured to match all four MetaTrader modes and accepts different price sources (close, open, high, low, median, typical, weighted).

## Trading logic

1. Wait for the current time to fall within the `[StartTime, StopTime)` trading window. Overnight windows are supported by wrapping around midnight.
2. Process only completed candles. Feed the configured moving average with the chosen applied price.
3. Store the previous moving average value to emulate the `iMA` shift logic used in MetaTrader.
4. When the previous average is below the latest price and the new average is above the price, open (or reverse into) a long position.
5. When the previous average is above the latest price and the new average is below the price, open (or reverse into) a short position.
6. Before opening a new position on the opposite side, flatten any existing exposure to mirror the `OrdersTotal() == 0` constraint of the original code.
7. Start a virtual stop-loss and take-profit with distances expressed in MetaTrader points multiplied by the current instrument `PriceStep`.

## Default parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeFrame(1m)` | Candle series that drives all calculations. |
| `MaPeriod` | `160` | Number of bars used by the moving average. |
| `MaMethod` | `Simple` | Moving average type: Simple, Exponential, Smoothed, or LinearWeighted. |
| `PriceType` | `Close` | Price source forwarded to the moving average (close/open/high/low/median/typical/weighted). |
| `StartTime` | `01:00` | Time of day when trading becomes active. |
| `StopTime` | `22:00` | Time of day when new entries stop. |
| `StopLossPoints` | `200` | MetaTrader points converted into an absolute protective stop distance. |
| `TakeProfitPoints` | `600` | MetaTrader points converted into an absolute profit target distance. |
| `OrderVolume` | `0.1` | Default volume submitted with market orders. |

## Notes

- If `StartTime` equals `StopTime`, the time filter is disabled and trading is allowed all day.
- When `StopLossPoints` or `TakeProfitPoints` equals zero, the corresponding protection level is not registered.
- The time filter uses the candle close time (`candle.CloseTime.TimeOfDay`) so it adapts to the exchange time zone supplied by MarketDataConnector.
- If the security does not expose `PriceStep`, point-based distances are used directly without scaling.

## Original strategy reference

- Source: `MQL/44133/MA Price Cross.mq4`
- Author: JBlanked (2023)
