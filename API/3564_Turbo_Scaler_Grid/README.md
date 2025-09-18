# Turbo Scaler Grid Strategy

## Overview
The Turbo Scaler Grid strategy is a high-level StockSharp implementation of the MQL5 "Turbo Scaler Grid Pending" expert advisor. The strategy focuses on managing pending stop grids around predefined price levels, dynamically protecting open positions with break-even and trailing logic, and supervising the account equity to close positions when profit or loss thresholds are reached.

The logic works on multiple timeframes simultaneously:

- A configurable trigger timeframe watches for price proximity signals that activate the pending grid.
- Additional 30-minute, 2-hour, and daily candles provide confirmation for optional conditional triggers.
- Level1 data supplies the latest bid/ask values used to position pending orders and to manage trailing stops.

## Trading Rules
1. **Pending grid**
   - Buy stop and sell stop orders are placed from configurable anchor prices (`BuyStopEntry` and `SellStopEntry`).
   - Orders are spaced by `PendingStepPoints` and limited by `PendingQuantity`.
   - The price trigger checks recent candles on the trigger timeframe to confirm that price approached the anchor level with sufficient momentum.
   - The condition trigger validates additional multi-timeframe filters (daily block ranges, H2 and M30 candle direction, and mid-range level) before placing pending orders.
2. **Position protection**
   - Initial stop loss is calculated from `StopLossPoints` (or fixed price overrides).
   - When price advances by `BreakevenTriggerPoints`, the stop is moved to the entry price plus `BreakevenOffsetPoints` (for longs) or minus the offset (for shorts).
   - A trailing stop activates only after break-even is reached, updating once price exceeds the previous stop by `TrailMultiplier * TrailPoints`.
3. **Equity supervision**
   - The strategy monitors floating PnL and forces position liquidation if the drawdown exceeds `MaxFloatLoss` (scaled to the selected order volume).
   - A floating profit trigger locks in gains by placing an internal equity line at `EquityBreakeven` and trailing it by `EquityTrail` once the profit surpasses `EquityTrigger`.

## Parameters
| Name | Description |
| --- | --- |
| `StopLossPoints` | Initial stop-loss distance in points. |
| `BreakevenTriggerPoints` | Points required to activate the break-even move. |
| `BreakevenOffsetPoints` | Offset added to the entry price when the stop is moved to break-even. |
| `TrailPoints` | Distance used for trailing after break-even. |
| `TrailMultiplier` | Multiplier applied before a new trailing stop is set. |
| `BuyStopLossPrice` / `SellStopLossPrice` | Optional fixed stop prices for long/short positions. |
| `BuyStopEntry` / `SellStopEntry` | Base prices for the pending stop grids. |
| `OrderVolume` | Volume per pending order. |
| `PendingQuantity` | Maximum number of active pending orders. |
| `PendingStepPoints` | Distance between consecutive pending orders. |
| `TriggerCandleType` | Candle series used for the price trigger logic. |
| `PendingPriceTrigger` | Enables the price proximity trigger. |
| `PendingConditionTrigger` | Enables the multi-timeframe confirmation trigger. |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | Daily low block used to validate long setups. |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | Daily high block used to validate short setups. |
| `MaxFloatLoss` | Maximum allowed floating loss (scaled by volume). |
| `EquityBreakeven` | Equity level maintained after the profit trigger activates. |
| `EquityTrigger` | Floating profit required to create the equity lock. |
| `EquityTrail` | Trailing distance applied to the equity lock. |

## Notes
- The order volume is scaled to match the original EA behaviour (`0.01` lots are treated as the base step).
- All comments inside the code are written in English, while this document provides a detailed description for quick onboarding.
- The strategy uses only high-level StockSharp APIs (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, `SellMarket`, `BuyMarket`) in line with the project requirements.
