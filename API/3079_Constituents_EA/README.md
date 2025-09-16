# Constituents EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **Constituents EA** from `MQL/22595` into the StockSharp high-level API. It recreates the original
logic of placing two pending orders around the most recent range at a specific hour while keeping the workflow compatible with
StockSharp order handling and risk protection helpers.

## How the Strategy Works

1. **Scheduled activation** – at the end of each candle the strategy checks whether the next bar will start at `StartHour`. Only
   at that moment are fresh pending orders considered, which mirrors the MetaTrader code that reacted to the birth of the bar
   whose open time matches the configured hour.
2. **Range detection** – the highest high and lowest low among the previous `SearchDepth` completed candles are tracked with
   `Highest`/`Lowest` indicators. These two prices define the breakout/mean-reversion levels used for order placement.
3. **Price distance filters** – current best bid/ask quotes are streamed from the order book feed. Orders are placed only if the
   distance between the quote and the candidate price is greater than or equal to `MinOrderDistancePips` (converted to absolute
   price using `PointValue`). This reimplements the original freeze-level validation and prevents invalid pending orders.
4. **Order style selection** – `PendingOrderMode` chooses between limit orders (buy limit at the low, sell limit at the high) or
   stop orders (buy stop above the high, sell stop below the low). Both orders are submitted simultaneously, just like in the
   MetaTrader script.
5. **Risk protection** – the built-in `StartProtection` helper attaches stop-loss and take-profit levels expressed in absolute
   price steps (`StopLossPips`/`TakeProfitPips`). Minimum-distance checks against `MinStopDistancePips` replicate the MT5
   requirement that protective orders must respect the symbol stop level.
6. **Order management** – if one pending order fills, the opposite order is cancelled immediately. During the bar interval the
   strategy never places additional orders as long as active ones exist, matching the source EA behaviour.

## Parameters

| Parameter | Description |
| --- | --- |
| `StartHour` | Hour (0-23) when the new pair of pending orders is created. |
| `SearchDepth` | Number of previous completed candles used to compute the high/low range. |
| `PendingOrderMode` | `Limit` replicates the mean-reversion variant, `Stop` places breakout orders. |
| `StopLossPips` | Stop-loss distance measured in pips (converted with `PointValue`). Set to 0 to disable. |
| `TakeProfitPips` | Take-profit distance in pips. Set to 0 to disable. |
| `PointValue` | Pip value in price units. Set to 0 to auto-detect from `Security.PriceStep`/`MinStep`. |
| `MinOrderDistancePips` | Minimal allowed distance between current bid/ask and the pending price, modelling freeze-level checks. |
| `MinStopDistancePips` | Minimal allowed stop/take distance, mirroring `StopsLevel` checks. |
| `CandleType` | Timeframe used for the range calculation and scheduling logic. |

`Strategy.Volume` controls the order size; keep it positive so that `BuyLimit`, `SellLimit`, `BuyStop`, and `SellStop` can submit
orders.

## Usage

1. Attach the strategy to a security and set `CandleType` to the timeframe you want to trade.
2. Configure `StartHour` and `SearchDepth` exactly as in the MT5 inputs. Adjust the `Min*Pips` thresholds if the broker enforces
   minimum distances between orders and the market price.
3. Calibrate `PointValue` when auto-detection from the security metadata is not possible (for example, on synthetic instruments).
4. Set `StopLossPips` and `TakeProfitPips` to match the original EA. The protection module will automatically attach stops and
   targets once an order fills.
5. Provide a positive `Volume` and start the strategy. It will subscribe to candles and order book data, place both pending orders
   at the scheduled bar, and cancel the opposite order whenever one trade is executed.

## Differences from the Original EA

- The MetaTrader `MoneyFixedMargin` risk mode (percentage-based sizing) is not ported. StockSharp users should configure
  `Strategy.Volume` directly or wrap the strategy with an external position sizing module.
- Freeze-level and stop-level checks are expressed through the configurable `MinOrderDistancePips` and `MinStopDistancePips`
  parameters because the equivalent exchange metadata is not always available via StockSharp.
- Order placement occurs when the prior candle closes and the upcoming bar starts at `StartHour`. This is functionally identical
  to the MT5 implementation that triggered on the birth of the new bar.
- All comments inside the source have been translated into English, while the external documentation is available in three
  languages for convenience.

Tune the distances and trading hour to match the instrument you plan to trade. On markets with wide spreads you may need larger
`MinOrderDistancePips` or pip values to avoid immediate rejection by the broker.
