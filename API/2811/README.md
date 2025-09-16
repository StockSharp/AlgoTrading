# Universal Trailing Manager Strategy

## Overview

The **Universal Trailing Manager Strategy** is a C# conversion of the MetaTrader expert advisor “Universal 1.64 (barabashkakvn's edition)”.
It automates trade management tasks for discretionary or semi-automatic trading by handling scheduled entries, grid-like pending
orders, dynamic trailing for market and pending orders, quick profit scalping, and portfolio-level notifications when account equity
moves by a defined percentage.

The strategy is designed to run on any instrument that exposes candle data. It does not rely on indicators; instead it reacts to
price levels and time windows, making it suitable for manual signal confirmation or integration into larger trade management
workflows.

## Key Features

- **Scheduled actions**: automatically open market positions or place pending orders at a specific terminal time (hour/minute).
- **Pending order grid**: maintains up to one buy limit, sell limit, buy stop, and sell stop order, each with independent offsets,
  optional trailing, and automatic re-registration when price moves in favour of the pending order.
- **Market position protection**: applies stop-loss, take-profit, and trailing logic to the current aggregated position, including the
  option to wait for unrealised profit before trailing begins.
- **Scalping exit**: closes existing positions once price advances by a fixed number of points from the average entry price.
- **Portfolio alerts**: monitors portfolio equity and logs messages when the account grows or declines by the configured percentage.
- **Position gating**: supports “wait until position is closed” mode as well as a configurable limit on the number of open positions
  per direction before accepting new entries or pending orders.

## Parameters

| Group | Parameter | Description |
|-------|-----------|-------------|
| General | `TradeVolume` | Order volume in lots used for market and pending entries. |
| General | `WaitClose` | When `true`, new orders are allowed only if the number of open positions in that direction is below `MaxMarketPositions`. |
| Market | `MaxMarketPositions` | Maximum number of active positions per direction when `WaitClose` is enabled. |
| Market | `MarketTakeProfitPoints` | Take-profit distance (in price points) applied to open positions. Set to 0 to disable. |
| Market | `MarketStopLossPoints` | Stop-loss distance (in price points) applied to open positions. Set to 0 to disable. |
| Market | `MarketTrailingStopPoints` | Trailing-stop distance (in price points). Set to 0 to disable trailing. |
| Market | `MarketTrailingStepPoints` | Minimal improvement (in points) required before the trailing stop is moved. |
| Market | `WaitForProfit` | When enabled, trailing starts only after profit exceeds `MarketTrailingStopPoints`. |
| Market | `ScalpProfitPoints` | Profit threshold (in points) that triggers an immediate position close. Set to 0 to disable scalping. |
| Pending | `AllowBuyLimit`, `AllowSellLimit`, `AllowBuyStop`, `AllowSellStop` | Master switches for each pending order type. |
| Pending | `LimitOrderOffsetPoints`, `StopOrderOffsetPoints` | Distance from current close price to place the corresponding limit/stop order. Must be above the instrument's minimum stop distance. |
| Pending | `LimitOrderTakeProfitPoints`, `StopOrderTakeProfitPoints` | Profit target (points) attached to newly opened positions created by pending orders. |
| Pending | `LimitOrderStopLossPoints`, `StopOrderStopLossPoints` | Protective stop (points) attached to newly opened positions created by pending orders. |
| Pending | `LimitOrderTrailingStopPoints`, `StopOrderTrailingStopPoints` | Trailing distance for active pending orders. Zero disables the trailing logic. |
| Pending | `LimitOrderTrailingStepPoints`, `StopOrderTrailingStepPoints` | Minimal improvement required before a pending order is moved while trailing. |
| Time | `UseTime` | Enables the scheduled action block. |
| Time | `TimeHour`, `TimeMinute` | Terminal time when the scheduled block is evaluated. |
| Time | `TimeBuy`, `TimeSell` | Open market buy/sell positions at the scheduled time. |
| Time | `TimeBuyLimit`, `TimeSellLimit`, `TimeBuyStop`, `TimeSellStop` | Place the corresponding pending order at the scheduled time regardless of the main permission switches. |
| Global | `UseGlobalLevels` | Enables portfolio-level monitoring. |
| Global | `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Equity percentage thresholds that trigger informational log messages. |
| Data | `CandleType` | Candle type used for periodic processing (default: 1 minute). |

## Execution Flow

1. **Candle arrival**: On each finished candle the strategy updates order references, synchronises scheduled signals, and evaluates
   trading logic.
2. **Time window**: If the candle close matches the configured time window, the appropriate booleans (`TimeBuy`, etc.) are set and
   market/pending orders are registered immediately.
3. **Pending orders**: The strategy places one pending order per type. When price movement satisfies the trailing rules, the order is
   cancelled and re-issued closer to the market with preserved offset.
4. **Market protection**: For open positions the strategy maintains dedicated stop-loss and take-profit orders, adjusting them based on
   trailing configuration and ensuring volumes match the aggregated position.
5. **Scalping check**: If `ScalpProfitPoints` is positive, the position is closed when the current close price reaches the target delta
   from the average position price.
6. **Global alerts**: Portfolio equity is checked every cycle; informative messages are logged once thresholds are reached.

## Usage Notes

- Place the strategy inside a trading scheme where candles are continuously delivered (for example, 1-minute candles). The logic is
  candle-driven, so a finer timeframe yields more responsive trailing.
- The strategy uses the aggregated `Position` property. When reversing from short to long (or vice versa) the executed order size is
  automatically increased to flatten the existing position before opening the new one.
- Pending order offsets and trailing steps are measured in *price points* (multiples of `Security.PriceStep`). Ensure the instrument's
  step value is configured correctly; otherwise the strategy falls back to a step size of 1.
- Global profit/loss monitoring provides informational log messages only. It does not automatically close positions; this mirrors the
  behaviour of the original expert advisor.
- When `WaitClose` is enabled, the number of open positions per side is derived from the aggregated position divided by `TradeVolume`.
  Use consistent volume sizes to obtain accurate gating behaviour.

## Logging

Every significant action—order placement, trailing adjustments, and global level alerts—is written to the strategy log via `LogInfo`.
Monitor the log to trace the decision process, especially while tuning offsets and trailing parameters.

