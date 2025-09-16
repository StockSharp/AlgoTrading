# Stoch Levels Strategy

## Overview
The **Stoch Levels Strategy** is a direct conversion of the MetaTrader 4 expert advisor `Stoch.mq4`. The original script relies on daily session boundaries, calculates custom price levels from the previous candle and places two pending orders for the upcoming session. This C# version keeps the same trading idea and implements it with StockSharp's high-level strategy API.

The strategy computes a synthetic trading range by expanding the previous candle's high/low spread by a configurable multiplier (default `1.1`). It then positions:

- A **sell limit** order above the prior close at half of the expanded range.
- A **buy limit** order below the prior close at half of the expanded range.

Whenever a pending order is filled, the strategy immediately attaches bracket exits (stop-loss and take-profit) using the distances defined in price steps. All outstanding exposure and pending orders are cleared at the beginning of every new trading day, mirroring the midnight reset block from the MQL script.

## Trading Logic
1. Subscribe to the configured candle series (daily by default) and wait for fully finished candles.
2. When a new session arrives:
   - Close any open position and cancel all protective or entry orders.
   - Compute the expanded range `range * RangeMultiplier` using the previous candle.
   - Place fresh sell and buy limit orders at `Close + range / 2` and `Close - range / 2` respectively.
3. On order fill, create matching stop-loss and take-profit orders using the requested price-step offsets.
4. If either protective order triggers, cancel the sibling protective order and wait for the next session reset.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `TakeProfitPoints` | Take-profit distance measured in price steps. | `20` | Equivalent to `TakeProfit` input in the MQL script. Set to `0` to disable the take-profit order. |
| `StopLossPoints` | Stop-loss distance measured in price steps. | `40` | Equivalent to `StopLoss` input in the MQL script. Set to `0` to disable the stop-loss order. |
| `RangeMultiplier` | Multiplier applied to the previous candle range (`High - Low`). | `1.1` | Matches the hard-coded `1.1` expansion factor in MQL. |
| `OrderVolume` | Volume for each pending order. | `1` | Mirrors the `Lots` parameter. |
| `CandleType` | Candle series that defines the trading session. | `Daily` | Customize if the strategy should operate on other timeframes. |

All parameters are configured via `Param()` to support optimization and UI binding.

## Risk Management
- Long entries receive a protective **sell stop** and **sell limit** bracket; shorts get the mirrored **buy stop** and **buy limit** exits.
- Orders are sized using `OrderVolume`. When one side of the bracket executes, the remaining protective order is cancelled to avoid duplicate exits.
- A full flat reset occurs on every new candle, ensuring the strategy does not carry exposure beyond the current session.

## Conversion Notes
- The MQL implementation used MetaTrader global variables to prevent duplicate orders; the C# version tracks the last processed session internally (`_lastProcessedDay`).
- The overnight closing loop has been translated into the `ResetOrders()` helper which cancels all pending orders and submits a market flatten command if a position remains.
- Stop-loss and take-profit levels are recreated explicitly through StockSharp order methods instead of being embedded into `OrderSend` parameters.
- Trailing stop, money management, and risk inputs present in the MQL script were unused there and remain unsupported in this port.

## Usage Tips
1. Attach the strategy to a security and set `OrderVolume`, stop distances, and candle type to match the traded instrument.
2. Ensure the security exposes a proper `PriceStep`; if not, the strategy falls back to `1` and logs a warning.
3. Because orders are recalculated only once per completed candle, keep the default daily timeframe to align with the original behaviour.
4. Review logs to confirm the daily reset, order placement, and protective order attachment workflow.

## Files
- `CS/StochLevelsStrategy.cs` – main strategy implementation.
- `README.md`, `README_cn.md`, `README_ru.md` – multilingual documentation for the converted strategy.
