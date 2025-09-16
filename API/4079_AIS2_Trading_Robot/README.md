# AIS2 Trading Robot 20005 (StockSharp port)

## Overview

AIS2 Trading Robot 20005 is an intraday breakout expert advisor originally written for MetaTrader 4. The port re-creates its multi-timeframe logic on top of StockSharp's high level strategy API. The strategy waits for momentum breakouts above/below the midpoint of the previous higher timeframe candle, applies dynamic take-profit and stop-loss distances derived from that candle's range, and manages positions with a secondary, faster timeframe that drives a trailing stop.

The conversion focuses on transparency and manual control: positions are opened with market orders, protective levels are enforced inside the strategy itself, and a configurable trading pause prevents rapid-fire re-entries. Equity-based position sizing mirrors the original "reserve" logic, allowing users to allocate a fraction of portfolio value to each trade while keeping a capital buffer untouched.

## Core Logic

1. **Primary timeframe analysis** – On each finished candle of the main timeframe (default 15 minutes) the strategy calculates:
   - Candle midpoint `(High + Low) / 2`.
   - Range-based take-profit and stop-loss distances (`range * TakeFactor` and `range * StopFactor`).
   - Current spread approximation, stop/freeze buffers, and a minimal trailing step.
2. **Breakout conditions** – Long entries require both a close above the midpoint and the current ask price breaking the previous high plus spread. Shorts mirror the condition for lows. Orders are blocked if the computed stop/target distances fail broker-level constraints.
3. **Risk management** – Position size is derived from portfolio equity: `OrderReserve` defines the tradable fraction, while `AccountReserve` keeps a portion untouched. If available capital or broker limits disallow the trade, the setup is skipped.
4. **Trade management** – The faster timeframe (default 1 minute) continuously updates the trailing distance. As price advances, the stop migrates in the trade's favor once the secondary range justifies it. Reaching either target or stop results in an immediate market exit.
5. **Operational guardrails** – A cooldown timer (`TradingPauseSeconds`) replicates the original MQL trading pause. The strategy also subscribes to the order book to capture live bid/ask values; when unavailable, it falls back to candle closes.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `PrimaryCandleType` | Higher timeframe used to generate entry signals. | 15-minute candles |
| `SecondaryCandleType` | Lower timeframe for trailing stop calculations. | 1-minute candles |
| `TakeFactor` | Multiplier applied to the primary candle range to build take-profit distance. | 1.7 |
| `StopFactor` | Multiplier applied to the primary candle range to build stop-loss distance. | 1.7 |
| `TrailFactor` | Multiplier applied to the secondary candle range for trailing updates. | 0.5 |
| `AccountReserve` | Fraction of equity held in reserve (not used for trading). | 0.20 |
| `OrderReserve` | Fraction of total equity allocated per trade before buffers. | 0.04 |
| `BaseVolume` | Fallback trade volume when risk sizing cannot be computed. | 1 lot |
| `StopBufferTicks` | Extra ticks added to broker stop-level compliance checks. | 0 |
| `FreezeBufferTicks` | Extra ticks preventing frequent stop updates near freeze levels. | 0 |
| `TrailStepMultiplier` | Multiplier applied to spread when validating trailing steps. | 1 |
| `TradingPauseSeconds` | Cooldown between consecutive trades. | 5 seconds |

All numeric parameters expose `SetCanOptimize()` (where meaningful) so they can participate in StockSharp optimization scenarios.

## Usage Notes

- Attach the strategy to a security and ensure Level1/order book data are available for accurate spread detection. Without live quotes, the logic still executes using candle closes, but stop validations become conservative.
- Set `PrimaryCandleType`/`SecondaryCandleType` to timeframes that exist in your data feed. The port uses `SubscribeCandles` and binds handlers through StockSharp's high level API.
- The trailing stop is virtual (managed internally); no stop orders are sent to the broker. If you require server-side stops, extend the code to register protective orders after entries.
- `StartProtection()` is called on start so the engine will liquidate unexpected positions if necessary.

## Differences from the Original EA

- The MetaTrader version manipulated terminal-wide global variables; this port keeps parameters inside the strategy and exposes them via `StrategyParam` wrappers.
- Order modifications were replaced with direct market exits because StockSharp handles stop/target logic within the algorithm itself.
- Risk calculations operate on portfolio equity supplied by StockSharp rather than account balance queries from MT4.

## Files

- `CS/Ais2TradingRobot20005Strategy.cs` – Strategy implementation using StockSharp high-level API.
- `README.md` – English description (this file).
- `README_cn.md` – Chinese translation.
- `README_ru.md` – Russian translation.

