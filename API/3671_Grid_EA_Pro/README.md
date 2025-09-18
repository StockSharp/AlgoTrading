# Grid EA Pro Strategy

## Overview
The **Grid EA Pro Strategy** reproduces the core behaviour of the original MetaTrader 4 expert advisor. The strategy combines grid-based scaling with RSI or timed breakout entries and virtual risk management features such as break-even and trailing stops. It is designed for netted portfolios, meaning it always works with a single net position and automatically clears the opposite direction when a new trade is opened.

## Trading Logic
- **Entry mode** – choose between RSI thresholds, time-driven breakouts or fully manual operation. In manual mode the strategy only manages existing positions and grid scaling.
- **Directional filter** – restrict trading to long, short or both directions.
- **Grid scaling** – after the initial entry the strategy can add positions when price retraces by a configurable number of points. Both the step and the order volume can grow geometrically.
- **Risk controls** – virtual stop-loss, take-profit, break-even, trailing stop and session filters mirror the original expert advisor behaviour.
- **Overlap exits** – parameters are provided for completeness, but due to the netted position model both directions cannot be held simultaneously. The overlap logic therefore remains inactive and the levels are documented for forward compatibility.

## Parameters
| Name | Description |
| --- | --- |
| `Mode` | Allowed trade direction (Buy, Sell, Both). |
| `EntryMode` | Signal source (RSI, FixedPoints, Manual). |
| `RsiPeriod`, `RsiUpper`, `RsiLower` | RSI configuration used in RSI mode. |
| `CandleType` | Candle subscription for signals and risk management. |
| `Distance`, `TimerSeconds` | Breakout distance and refresh interval for fixed point entries. |
| `InitialVolume`, `FromBalance`, `Risk %` | Money management block. If `Risk %` > 0 the position size is derived from account equity and stop-loss distance, otherwise a balance-based or fixed lot is used. |
| `LotMultiplier`, `MaxLot` | Multiplier and cap for grid additions. |
| `Step`, `StepMultiplier`, `MaxStep` | Grid spacing settings in points. |
| `OverlapOrders`, `OverlapPips` | Reserved for hedged overlap logic (disabled in this implementation). |
| `Stop Loss`, `Take Profit` | Initial protective levels in points (`-1` disables). |
| `Break Even Stop`, `Break Even Step` | Move stop to breakeven after the price moves by the defined step. |
| `Trailing Stop`, `Trailing Step` | Trailing stop configuration. |
| `Start Time`, `End Time` | Trading session window in local platform time (HH:mm). |

## Charting
When the chart area is available the strategy plots price candles, the RSI line and all own trades, matching the layout of the source expert advisor.

## Notes
- The strategy automatically cancels pending breakout levels once they are filled or when the direction is disabled.
- Because StockSharp uses netted positions, only one side of the market can be open at a time. Opening a long position clears existing shorts and vice versa.
- Ensure the instrument properties (`PriceStep`, `StepPrice`) are configured so that point-based parameters match the original MT4 settings.
