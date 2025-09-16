# Colibri Grid Manager Strategy

## Overview
Colibri Grid Manager is a StockSharp port of the MetaTrader 4 expert advisor `Colibri.mq4` (original folder `MQL/9713`). The strategy focuses on discretionary grid trading: it prepares layered pending orders on demand, sizes each order using the configured risk budget, attaches protective exits, and enforces a daily drawdown limit before disabling further trading.

## Trading Logic
1. When the strategy starts it subscribes to the selected candle series and order book to keep track of reference prices, resets the daily profit base and clears previous orders.
2. If `EnableGrid` is true and no position or active grid orders exist, the strategy builds a fresh grid for each allowed direction (`AllowBuy`, `AllowSell`). Orders can be distributed around a manual center price or relative to explicit buy/sell entry anchors.
3. The order type (`OrderType`) controls whether the grid uses limit, stop or immediate market entries. The distance between levels is set in points via `LevelSpacingPoints` and converted to price increments using the instrument tick size.
4. Volume is either fixed (`FixedOrderVolume`) or derived from `RiskPercent`. The risk-based sizing allocates the configured percentage of current portfolio equity across all levels in one direction and divides it by the monetary risk implied by the protective stop.
5. Once an entry order fills, the strategy automatically places paired protective orders: stops are derived from `StopLossPrice` or `StopDistancePoints`, while take-profits rely on `TakeProfitDistancePoints` or default to one grid step away. Pending orders can expire after `ExpirationHours` hours.
6. The strategy continuously monitors realised plus floating PnL. If the loss of the current trading day breaches `DailyLossLimitPercent`, it cancels all orders, closes open positions and suspends new grid creation until the next day starts.
7. Manual toggles (`CloseAllPositions`, `CloseLongPositions`, `CloseShortPositions`, `CancelOrders`) allow the trader to flatten or clean the book instantly without touching code.

## Parameters
- **EnableGrid** – master switch that enables or disables automatic grid maintenance.
- **OrderType** – entry order type (`Limit`, `Stop`, `Market`) used when creating levels.
- **AllowBuy / AllowSell** – choose the sides that may participate in the grid.
- **UseCenterLine / CenterPrice** – when enabled, distribute buy/sell levels symmetrically around a central price; a zero center uses the mid price.
- **LevelSpacingPoints** – spacing between consecutive levels, measured in points and converted to absolute price differences via the instrument tick size.
- **LevelsCount** – number of levels per direction. For market mode only one order is sent regardless of this value.
- **BuyEntryPrice / SellEntryPrice** – explicit anchors for long and short grids when the center mode is disabled (zero defaults to the current bid/ask).
- **StopLossPrice** – absolute stop level applied to every order. Leave zero to derive the stop from `StopDistancePoints`.
- **StopDistancePoints** – fallback stop distance in points when no absolute stop price is provided.
- **TakeProfitDistancePoints** – optional take-profit distance in points. When zero the strategy uses one grid step as the default target.
- **UseRiskSizing / RiskPercent** – enable percentage-based sizing and define the portion of equity allocated to each directional grid. The value is split equally across all levels of that direction.
- **FixedOrderVolume** – order size used when risk-based sizing is disabled or fails to produce a valid volume.
- **ExpirationHours** – optional lifetime for pending grid orders.
- **DailyLossLimitPercent** – stop-trading threshold expressed as a fraction of the portfolio equity captured at the start of the trading day.
- **CloseAllPositions / CloseLongPositions / CloseShortPositions / CancelOrders** – manual maintenance commands accessible from the UI.
- **CandleType** – candle series used for maintenance events such as daily resets.

## Implementation Notes
- The strategy relies exclusively on the high-level StockSharp API: `SubscribeCandles`, `SubscribeOrderBook`, `BuyLimit`, `SellStop`, etc. No direct connector logic or indicator access is required.
- Order sizing uses `Security.PriceStep` and `Security.StepPrice` to translate point-based distances from the MQL script into monetary risk.
- Protective exits are implemented via separate stop/limit orders rather than modifying the original entry order, which matches the way StockSharp handles linked protection orders.
- The daily loss filter resets when the calendar day changes and the portfolio value is recorded again. Traders can resume trading manually by toggling `EnableGrid` if they want to override the safety lock.
- MT4 global variables, emergency closing flags and graphical clean-up routines from the source script were replaced with strongly typed parameters and manual toggles.

## Usage Tips
1. Define whether the grid should be centred or anchored to specific prices before enabling it. For centred grids, supply a meaningful `CenterPrice`; for anchored grids leave it disabled and fill in the buy/sell entry prices.
2. Calibrate `LevelSpacingPoints`, `StopDistancePoints` and `TakeProfitDistancePoints` to match the instrument volatility. Remember that all three are point-based values.
3. When using risk-based sizing, verify that the instrument has valid `PriceStep` and `StepPrice`; otherwise the strategy will fall back to the fixed volume.
4. Use the manual control parameters to cancel or flatten positions quickly before modifying configuration parameters.
5. Combine the daily loss limit with external risk management if several strategies share the same portfolio.

## Differences from the Original Expert Advisor
- The StockSharp version focuses on a clean parameter interface instead of MT4 global variables and comment-based magic-number logic.
- Emergency closure flags, grid size auto-adjustments, and graphical object clean-up from the original code are reduced to manual toggles and straightforward parameter validation.
- Trailing stop helpers from the MQL script are not replicated; use StockSharp’s existing trailing modules if required.
- The MQL dependency logic between orders (execute/cancel based on “mother” orders) is not reproduced. Each level operates independently with its own protection orders.

These adjustments keep the spirit of the original Colibri expert advisor—structured multi-level entries with strict money management—while aligning the implementation with idiomatic StockSharp patterns.
