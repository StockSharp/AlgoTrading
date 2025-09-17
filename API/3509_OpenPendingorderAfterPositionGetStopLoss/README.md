# OpenPendingorderAfterPositionGetStopLoss

## Overview
The **OpenPendingorderAfterPositionGetStopLoss** strategy ports the MetaTrader 5 expert advisor of the same name into the StockSharp high-level API. It continuously evaluates the slope of the Stochastic %K line on the selected timeframe. When %K turns down it places a sell stop order below the market, and when %K turns up it places a buy stop order above the market. Each filled entry immediately receives a protective stop-loss and take-profit order. If a stop-loss closes the position, the strategy automatically reinstalls the corresponding pending order so that the grid of breakout trades is restored without waiting for the next candle.

## Trading rules
- Subscribe to finished candles of the configured timeframe and compute a classic Stochastic oscillator (`KPeriod`, `DPeriod`, `Slowing`).
- Compare the current %K value with the value two bars ago:
  - `%K(current) < %K(two bars ago)` &rarr; submit a sell stop below the best bid.
  - `%K(current) > %K(two bars ago)` &rarr; submit a buy stop above the best ask.
- Pending orders are offset from the market by the current spread plus the user-defined `MinStopDistancePoints` buffer, matching the original MQL logic.
- Once a pending order fills, the strategy sends a protective stop-loss (stop order) and an optional take-profit (limit order).
- When the protective stop-loss fires, the corresponding pending order is recreated immediately using the latest market prices.
- Protective orders are cancelled automatically when the position is closed by the take-profit or when the strategy stops.

## Parameters
| Name | Description |
| --- | --- |
| `OrderVolume` | Trade volume in lots for each pending order. |
| `StopLossPoints` | Stop-loss distance in symbol points. Set to 0 to disable. |
| `TakeProfitPoints` | Take-profit distance in symbol points. Set to 0 to disable. |
| `MinStopDistancePoints` | Minimal price buffer (in points) added to the spread before placing a pending order. |
| `MaxPositions` | Maximum number of simultaneous positions per direction (netting accounts effectively use 0 or 1). |
| `KPeriod` | Number of bars used for the %K calculation. |
| `DPeriod` | Length of the smoothing %D line. |
| `Slowing` | Additional smoothing factor applied to %K before comparison. |
| `PendingExpiry` | Optional lifetime of pending stop orders. Expired orders are cancelled on the next candle. |
| `CandleType` | Timeframe used for candle subscription and indicator calculations. |

## Implementation notes
- All order management relies on high-level helpers such as `BuyStop`, `SellStop`, `SellLimit`, and `BuyLimit` as required by `AGENTS.md`.
- Indicator values are consumed directly inside the `SubscribeCandles().BindEx(...)` callback, avoiding any `GetValue` calls.
- The strategy monitors `MyTrade` events to install and remove protective orders, emulating the `OnTradeTransaction` logic from the original Expert Advisor.
- Comments inside the code are written in English and indentation is done with tabs, conforming to the repository guidelines.
