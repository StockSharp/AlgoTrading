# 21-Hour Session Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the MetaTrader "21hour" expert advisor inside StockSharp. It operates during two configurable trading windows and uses pending stop orders to capture breakouts at the top and bottom of the range. At the end of each window the strategy liquidates any open exposure and removes the working orders, ensuring that every trading day starts clean.

## Core Idea

- Trade direction is determined purely by price action around the specified session start times.
- At the beginning of each session the strategy brackets the market with a buy stop above the current ask and a sell stop below the current bid.
- When a stop order fills the opposite side is cancelled immediately and a fixed-distance take-profit order is placed.
- At the configured session end time every position is closed and all orders are cancelled, even if the take-profit has not been reached yet.

## Data Flow

- **Candles:** 1-minute candles (configurable) are used only to provide time stamps and to fire the hourly schedule checks.
- **Order book:** Level 1 quotes supply the current best bid/ask values that define the stop order activation prices.

## Trading Rules

### Entry Scheduling
- At `FirstSessionStartHour` (default 08:00 server time) and at `SecondSessionStartHour` (default 22:00) the strategy:
  - Places a buy-stop at `Ask + StepPoints * PriceStep`.
  - Places a sell-stop at `Bid - StepPoints * PriceStep`.
- Only one position is allowed. If a position is already open when the other session starts, all pending entry orders are removed before new ones are placed.

### Order Management
- When one of the stop orders is filled the opposite stop is cancelled immediately.
- A take-profit limit order is registered at `EntryPrice ± TakeProfitPoints * PriceStep` depending on the trade direction.
- Order sizes are fixed by the `Volume` parameter (defaults to 1 lot).

### Exit Logic
- Take-profit orders close winning trades automatically.
- At `FirstSessionStopHour` (default 21:00) and `SecondSessionStopHour` (default 23:00) the strategy closes any open position at market and cancels all remaining pending orders.
- If the position is flattened manually, the strategy also removes the outstanding take-profit order.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `1` | Order volume used for both stop entries and take-profit exits. |
| `FirstSessionStartHour` | `8` | Hour (0-23) when the first trading session begins. |
| `FirstSessionStopHour` | `21` | Hour when the first session ends and positions are closed. |
| `SecondSessionStartHour` | `22` | Hour when the evening session begins. Must be after the first session. |
| `SecondSessionStopHour` | `23` | Hour when the second session ends. Must be after the first session stop. |
| `StepPoints` | `5` | Distance from the best quote to the entry stop order, measured in price steps. |
| `TakeProfitPoints` | `40` | Distance between the entry price and the take-profit limit, measured in price steps. |
| `CandleType` | `1 minute` | Candle type used to drive the intraday schedule checks. |

All parameters are validated to avoid overlapping sessions or impossible hour combinations.

## Tags & Characteristics

- **Style:** Session breakout / time-based trend following.
- **Direction:** Long and short.
- **Timeframe:** Intraday, schedule-driven (1-minute candles for timing only).
- **Risk Controls:** Fixed take-profit plus forced flat at session end (no stop-loss).
- **Market Types:** Designed for FX, indices, or any instrument with continuous trading hours and reliable quotes.
- **Complexity:** Low – no indicators, purely time and price based.

## Implementation Notes

- The strategy requires a valid `Security.PriceStep`; orders are skipped if price step or quotes are not available.
- Take-profit volumes use the executed trade volume when available, falling back to the current position or configured volume.
- The code keeps English inline comments for clarity and mirrors the original MQL logic while leveraging StockSharp high-level APIs (`SubscribeCandles`, `SubscribeOrderBook`, helper parameters, and order helpers).
