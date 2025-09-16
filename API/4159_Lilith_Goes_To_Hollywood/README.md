# Lilith Goes To Hollywood Strategy

## Overview
This strategy recreates the behaviour of the MetaTrader expert "Lilith goes to Hollywood" inside the StockSharp high level API. It implements a hedging grid that can operate in two very different modes:

* **Automated mode** – Parabolic SAR triggers immediate market entries whenever price crosses the stop-and-reverse value.
* **Manual mode** – Pending stop/limit orders are parked around user-defined reference prices and left to fill.

In both cases the strategy keeps track of the long and short exposure separately, calculates the floating PnL of the open grid and uses that information to decide when to deploy additional recovery orders.

## Operating modes
* **Automated** – When no position is open the strategy subscribes to the Parabolic SAR indicator (0.02/0.2 factors). If the candle close is above the indicator it buys at market, if it is below it sells. The executed price becomes the new **focus** and recovery stops are armed at a configurable anchor distance around it.
* **Manual** – When no position is open the strategy submits a single pending order per side. If the market trades below the buy level a buy stop is created, otherwise a buy limit is submitted. The sell side mirrors the same logic around the `PriceDown` level. Once one of the orders fills the other side remains active until cancelled manually or by the strategy.

## Order management logic
* The grid keeps running totals of filled long/short volumes and pending buy/sell orders. This allows the strategy to measure imbalances between both sides of the book.
* Whenever the floating profit reaches the dynamic target (`account value / 1000`) the strategy closes every position and cancels all pending orders.
* If the floating PnL drops below `-AccountValue * RiskPercent / 100`, an emergency hedge is deployed by opening market orders that cover the net short or long excess.
* Recovery orders are expressed as stop orders placed around the focus price (automated mode) or around the configured manual prices. Their size is calculated as `(opposite exposure * XFactor) - current exposure`, mimicking the MT4 logic of oversizing the next order to rebalance the grid.

## Parameters
| Name | Description |
| --- | --- |
| `Automated` | Enables Parabolic SAR driven market entries. Disable to work in manual pending order mode. |
| `PriceUp` | Reference price used to create buy stop/limit orders in manual mode. |
| `PriceDown` | Reference price used to create sell stop/limit orders in manual mode. |
| `AnchorSteps` | Distance, expressed in price steps, used to offset recovery orders from the focus price. |
| `ManualVolume` | Base lot size when operating manually or when the dynamic position sizing produces zero. |
| `XFactor` | Multiplier applied to the opposing exposure when sizing recovery orders. |
| `RiskPercent` | Maximum floating loss (percentage of the account value) tolerated before the strategy deploys an emergency hedge. |
| `CandleType` | Time-frame used to drive the Parabolic SAR and general management logic. |

## Risk controls
* Profit taking is dynamic and scales with the account value, providing an automatic way to raise the target as the account grows.
* Emergency hedging can neutralise extreme drawdowns by flattening the most exposed side of the grid once the floating loss exceeds the `RiskPercent` threshold.
* All pending orders are rounded to the instrument tick size and volumes are adjusted to respect exchange limits, matching the typical protections of the original MetaTrader expert.

## Conversion notes
* MetaTrader ticks are replaced with finished candles. The default one-minute timeframe keeps the strategy reactive, yet it can be adjusted via the `CandleType` parameter.
* The `Anchor` setting from the MQL source expressed the distance in points. Here it is configured as a number of price steps so it adapts to the instrument tick size automatically.
* The original "Comment" output has been converted into strategy log messages (`LogInfo`) so the platform journal contains the same feedback without relying on chart annotations.
