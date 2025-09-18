# MTrendLine Strategy

## Overview
The **MTrendLine Strategy** ports the MetaTrader script `MTrendLine.mq4` to StockSharp's high-level strategy API. The original
expert advisor repeatedly adjusts the price of existing pending orders so they remain aligned with a trend line drawn on the
chart. The StockSharp version automates the same behaviour by rebuilding the moving trend line with a configurable
`LinearRegression` indicator. Up to three independent pending order slots can follow the calculated regression line with their
own order type, distance, and volume. Every time a new candle closes the strategy recomputes the line value, evaluates the
required offsets, and refreshes the pending orders accordingly.

The port adds modern risk and usability improvements such as structured parameters, automatic conversion from MetaTrader points
into real price steps, and optional stop-loss / take-profit distances that move together with the pending orders. Bid/ask
updates are monitored via `SubscribeLevel1()` so the strategy honours the minimal distance that brokers demand between the
current market price and resting orders.

## Trading logic
1. Subscribe to the configured candle series through `SubscribeCandles()` and feed a `LinearRegression` indicator with each
   finished bar. The indicator represents the manual trend line from the MetaTrader version.
2. Maintain Level1 subscriptions to cache the latest best bid and best ask values. They are used to enforce the minimum
   distance parameter before relocating a pending order.
3. For every enabled slot compute the desired price as **regression value + distance × point size**. The point size defaults to
   the security price step but can be overridden to match MetaTrader's `Point` constant.
4. Convert the slot configuration into StockSharp order helpers (`BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`). Optional
   stop-loss and take-profit prices are derived from the requested distance in points so they track the order after each move.
5. If an active pending order already exists for the slot and the new target price differs, cancel the current order first and
   wait for the next candle to place the updated one. This mirrors the behaviour of `OrderModify` from the MQL code without
   risking duplicate requests.
6. When a slot is disabled or the computed price becomes invalid (for example, negative), cancel the associated pending order
   and clear its cached state.

## Pending order slots
Each slot emulates one call to `modify()` in the original EA. Slots can be configured independently:
- **Type** — choose between Buy Limit, Buy Stop, Sell Limit, or Sell Stop.
- **Distance** — distance in MetaTrader points added to the regression value to obtain the new price. Use negative values to
  position orders below the regression line.
- **Volume** — size of the pending order. If set to zero or negative, the strategy falls back to the global `TradeVolume`.
- **Enable flag** — allows disabling a slot without removing its configuration. Disabled slots automatically cancel any active
  orders that belong to them.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-hour candles | Primary timeframe used to build the regression trend line. |
| `RegressionLength` | `int` | `24` | Number of completed candles fed into the `LinearRegression` indicator. |
| `PointValue` | `decimal` | `0` | Monetary value of one MetaTrader point. When zero the strategy uses the security price step. |
| `TradeVolume` | `decimal` | `1` | Default volume used by all slots when their own volume is zero. |
| `StopLossPoints` | `decimal` | `0` | Stop-loss distance in points. Set to zero to disable automatic stop-loss placement. |
| `TakeProfitPoints` | `decimal` | `0` | Take-profit distance in points. Set to zero to disable automatic take-profit placement. |
| `MinDistancePoints` | `decimal` | `0` | Minimum gap (in points) that must exist between the best bid/ask and the pending order. |
| `PendingOrder{1,2,3}Enabled` | `bool` | Slot specific | Enables or disables the given slot. |
| `PendingOrder{1,2,3}Mode` | `enum` | Slot specific | Pending order type: BuyLimit, BuyStop, SellLimit, or SellStop. |
| `PendingOrder{1,2,3}DistancePoints` | `decimal` | Slot specific | Distance (in points) added to the regression value to compute the order price. |
| `PendingOrder{1,2,3}Volume` | `decimal` | Slot specific | Volume for the slot. Zero reuses `TradeVolume`. |

## Differences from the original MetaTrader script
- MetaTrader modifies existing orders in place. StockSharp uses cancel-and-replace semantics while waiting for confirmation
  before registering the replacement order on the next candle.
- The original code reads the value of a manually drawn trend line. The port replaces this with an automatic
  `LinearRegression` indicator so the behaviour is deterministic and can run unattended.
- `MODE_STOPLEVEL` is not available on StockSharp. Instead, the strategy provides the configurable `MinDistancePoints`
  parameter and enforces it using real-time bid/ask updates.
- Stop-loss and take-profit distances are optional parameters instead of reading existing order settings. This keeps the values
  consistent across order re-registrations.

## Usage tips
- Set `PointValue` to match the broker's point definition if it differs from the security `PriceStep`; this guarantees the
  distance parameters mirror their MetaTrader counterparts.
- Enable only the slots you need. Each slot maintains its own pending order and comment (`"MTrendLine slot N"`) so identifying
  them in reports or the Order Log is straightforward.
- Consider combining the strategy with StockSharp's built-in risk protection helpers if you require trailing stops or account
  level controls. The implementation focuses on mirroring the original order-modification logic.

## Indicators
- `LinearRegression` applied to finished candles.
