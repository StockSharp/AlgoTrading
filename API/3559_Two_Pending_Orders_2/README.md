# Two Pending Orders 2

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor **"Two pending orders 2"**. It keeps two symmetric pending orders around the market price and lets the first triggered side manage the trade with configurable stop-loss, take-profit, and trailing rules. The conversion uses the high level StockSharp API and keeps the core ideas of the original algorithm while exposing every tuning knob through strategy parameters.

## Trading logic
1. The strategy subscribes to the selected candle series (daily candles by default). When a candle is finished it becomes the decision point for the next trading cycle.
2. Active pending orders are cancelled once they expire or before new orders are placed. This guarantees there are only the freshest levels in the market.
3. If the current spread is within the allowed threshold and the active position/order count is below the configured limit, the strategy places two symmetric pending orders:
   - **Stop mode** (default) places a buy stop above the market and a sell stop below it.
   - **Limit mode** places a buy limit below the market and a sell limit above it.
   - The *Reverse Levels* flag swaps the order types to replicate the original EA reverse switch.
4. Entry prices are offset from the current bid/ask by the *Pending Indent* parameter. Orders are skipped when they are closer than the *Min Step* distance to existing positions.
5. Pending orders can expire after a given number of minutes. When expiration is reached all remaining orders are cancelled.

## Position management
- Once an order is filled the strategy tracks the average entry price and volume for the corresponding side. Opposite fills reduce or close the existing position before opening a new one.
- The strategy exits long positions when price hits any of these conditions:
  - Price touches the stop-loss distance below the average entry price.
  - Price reaches the take-profit distance above the average entry price.
  - A trailing stop is activated after the profit exceeds the activation threshold and subsequently price pulls back to the trailing level (moved in steps).
- Short trades use the mirrored rules with inverted price comparisons.
- When *Only One Position* is enabled the engine waits for the current exposure to be closed before new pending orders are placed.

## Parameters
| Name | Description |
| --- | --- |
| `StopLossPoints` | Distance to the protective stop-loss in points (0 disables it). |
| `TakeProfitPoints` | Distance to the take-profit target in points (0 disables it). |
| `MaxPositions` | Maximum number of simultaneously active positions and pending orders. |
| `MinStepPoints` | Minimum distance between the entry price of existing trades and new pending orders. |
| `TrailingActivatePoints` | Profit threshold that activates the trailing stop (0 disables trailing). |
| `TrailingStopPoints` | Distance between the market price and trailing stop once activated. |
| `TrailingStepPoints` | Minimum price improvement required to move the trailing stop again. |
| `TradeMode` | Allowed direction for new pending orders: `Buy`, `Sell`, or `BuySell`. |
| `PendingType` | Type of pending orders to place: `Stop` or `Limit`. |
| `PendingExpirationMinutes` | Lifetime of pending orders in minutes (`0` keeps them until filled or cancelled manually). |
| `PendingIndentPoints` | Offset from the current bid/ask used to calculate pending order prices. |
| `PendingMaxSpreadPoints` | Maximum allowed spread between bid and ask to place pending orders (`0` disables the filter). |
| `OnlyOnePosition` | If `true`, prevents opening new trades until the current position is closed. |
| `ReverseLevels` | Swaps the placement of buy and sell orders to mirror the original EA reverse mode. |
| `CandleType` | Time frame used to trigger signal evaluation (daily by default). |

## Notes
- Price distances are expressed in points and automatically converted to the instrument tick size.
- The strategy relies on StockSharp helper methods (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) for order registration and uses `CancelActiveOrders` to reset the book each time a new decision is made.
- Trailing stop logic is evaluated on finished candles. For intrabar trailing behaviour use a shorter `CandleType`.
