# DeMarker Pending Strategy

## Overview

The **DeMarker Pending Strategy** converts the MetaTrader expert *DeMarker Pending.mq5* to the StockSharp high level API. The strategy observes the DeMarker oscillator on a configurable timeframe. When the oscillator dives below the lower band it prepares a long entry; when it climbs above the upper band it prepares a short entry. Instead of firing market orders instantly, the algorithm places stop or limit pending orders at an adjustable offset from the current bid/ask price. Once a pending order executes, the strategy manages the resulting position with configurable stop loss, take profit, and optional trailing logic.

## Trading Logic

1. **Indicator evaluation** – The DeMarker indicator with the chosen period is evaluated on completed candles only. Long signals appear below the lower level, short signals above the upper level.
2. **Spread guard** – Before placing an order the current spread (best ask minus best bid) must be below the maximum allowed threshold. The threshold is expressed in price steps for consistent behaviour across instruments.
3. **Pending order placement** –
   - In *Stop* mode the strategy places stop orders in the breakout direction at `ask + indent` for buys or `bid - indent` for sells.
   - In *Limit* mode it places limit orders on pullbacks at `bid - indent` for buys or `ask + indent` for sells.
   - The indent value is configured in price steps and automatically aligned to the instrument price step.
   - Depending on the settings the previous pending order can be cancelled before a new one is sent, or the strategy can keep only a single working pending order.
   - Optional order expiration automatically cancels unfilled orders after the configured lifetime.
4. **Session filter** – Trading can be limited to a daily session. Outside the allowed time window the strategy cancels working pending orders and ignores new signals.
5. **Fill management** – When a pending order is filled the strategy records the entry price and immediately sets up internal stop loss and take profit levels. Both distances are defined in price steps. A global profit target can close all positions and cancel any remaining pending orders once the accumulated PnL exceeds the configured value.
6. **Trailing stop** – After the position gains the configured activation distance the trailing logic tightens the stop. Additional progress (trailing step) can be required between adjustments to mimic the original expert behaviour.
7. **Position exit** – Stops, targets, and trailing levels are monitored using live Level 1 quotes. When breached the strategy exits using market orders. Pending orders are automatically cleared when the position closes.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Lot size for new pending orders. Must be positive. |
| `DeMarkerPeriod` | Averaging length of the DeMarker oscillator. |
| `DeMarkerUpperLevel` | Threshold above which short entries are prepared. |
| `DeMarkerLowerLevel` | Threshold below which long entries are prepared. |
| `StopLossPoints` | Protective stop distance in price steps. `0` disables the stop. |
| `TakeProfitPoints` | Take profit distance in price steps. `0` disables the target. |
| `TrailingActivationPoints` | Profit (in price steps) required before the trailing stop activates. `0` disables the activation requirement. |
| `TrailingStopPoints` | Trailing stop distance in price steps. `0` disables trailing. |
| `TrailingStepPoints` | Extra price steps required between trailing adjustments. `0` moves the stop on every update. |
| `PendingIndentPoints` | Offset from the reference price (bid/ask) when placing pending orders, in price steps. |
| `PendingMaxSpreadPoints` | Maximum allowed spread before orders are skipped, expressed in price steps. `0` disables the filter. |
| `PendingOnlyOne` | If `true`, the strategy keeps at most one active pending order. |
| `PendingClosePrevious` | If `true`, any working pending order is cancelled before a new one is submitted. |
| `PendingExpiration` | Lifetime of pending orders. Zero duration disables expiration. |
| `EntryMode` | Chooses between `Stop` and `Limit` pending orders. |
| `UseTimeFilter` | Enables the daily session filter. |
| `SessionStart` | Inclusive session start time used when the filter is enabled. |
| `SessionEnd` | Exclusive session end time used when the filter is enabled. Supports overnight sessions. |
| `TargetProfit` | Monetary profit target that closes all positions and cancels pending orders once reached. `0` disables the target. |
| `CandleType` | Candle timeframe used for the DeMarker indicator. |

## Notes

- The strategy subscribes to Level 1 quotes to monitor best bid/ask prices. Trailing stops and risk controls rely on those updates, therefore the data feed must provide timely Level 1 information.
- All distances defined in “points” are converted to price steps using the instrument `PriceStep`. This reproduces the point-based configuration of the MetaTrader version.
- The code uses only StockSharp high-level helpers (`BuyStop`, `SellLimit`, `SetDisplay`, etc.) to stay aligned with the project guidelines.
