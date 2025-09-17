# Trailing Activate Close All Strategy

## Overview

The **Trailing Activate Close All Strategy** is a risk management component that mirrors the behaviour of the original MetaTrader expert advisor *Trailing Activate Close All*. It does not open new trades. Instead, it manages positions that were opened manually or by other strategies on the same security. The module automatically attaches stop-loss and take-profit orders, trails the stop when a position becomes profitable, and can close all open positions once a target profit is reached.

The logic is designed for instruments quoted in fractional pips. All distance-related parameters are expressed in MetaTrader "points" (for example, the difference between 1.00055 and 1.00045 equals 10 points). The strategy converts those points into absolute price distances using the security price step.

## Features

- Places initial stop-loss and take-profit orders if they are missing.
- Optional trailing stop with activation and step thresholds.
- Option to recalculate protective orders on every tick or only on new candles of a selected timeframe.
- Automatic liquidation of all positions once aggregate unrealised profit reaches a user-defined target.
- Obeys exchange restrictions such as stop and freeze levels by applying a configurable safety multiplier.
- Produces detailed log messages for all major actions.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Candle Type** | Timeframe used to detect new bars when trailing on bar close. Ignored in tick mode. |
| **Trailing Mode** | Choose between recalculating on every tick (`EveryTick`) or only on finished candles (`NewBar`). |
| **Stop Loss (points)** | Distance from the current market price to the protective stop. Set to zero to disable automatic stop placement. |
| **Take Profit (points)** | Distance from the market price to the protective take profit. Set to zero to disable. |
| **Trailing Activate (points)** | Minimum profit (from the entry price) required before the trailing stop may start moving. |
| **Trailing Stop (points)** | Distance between the current price and the trailing stop once activated. Must be greater than zero to enable trailing. |
| **Trailing Step (points)** | Minimum price improvement before the stop can be moved again. |
| **Target Profit (money)** | Aggregate unrealised profit that triggers closing all positions. Set to zero to disable. |
| **Freeze Coefficient** | Multiplier used when the exchange does not report stop/freeze levels. The spread is multiplied by this coefficient to obtain a safety buffer. |
| **Detailed Logging** | Enables verbose log messages about modifications and forced liquidations. |

## Usage Notes

1. Assign the strategy to a security and portfolio that already hold positions or will receive entries from external logic.
2. Configure distance parameters in points to match the instrument tick size.
3. The strategy requires level 1 market data to obtain bid/ask prices and exchange limits. When `NewBar` mode is selected, it also subscribes to the chosen candle series.
4. When the trailing stop or target profit conditions are met, the strategy cancels existing protective orders before sending market exit orders to close the position.
5. If trailing is enabled but the step parameter is zero, the strategy stops immediately and reports a configuration error, matching the safety check found in the original expert advisor.

## Differences from the MQL Version

- Uses StockSharp protective orders (stop and limit) instead of MetaTrader `PositionModify` requests.
- Adapts distances to the security price step and optionally to the reported stop/freeze levels.
- Aggregates positions managed by the strategy rather than iterating over MetaTrader position tickets.
- Emits structured log messages for all major decisions.

This module can be combined with other StockSharp strategies to provide automatic risk management on any supported market.
