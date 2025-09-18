# Graal Fractal Channel Strategy

## Overview
The **Graal Fractal Channel Strategy** is a StockSharp port of the MetaTrader 4 expert advisor "Graal-003". The algorithm watches five-candle fractal patterns and confirms breakouts using adaptive price channels. When a valid bullish or bearish fractal appears, the strategy evaluates several filters (fractal tunnel, close-price envelope, and optional flat-market suppression) before entering in the breakout direction. An optional Williams %R overlay replicates the manual exit logic from the original robot, while hedge stop orders can be staged to emulate the EA's counter-trend protection.

## Data flow and indicators
* Subscribes to the configured `CandleType` (hourly candles by default).
* Builds a rolling queue of the last `ChannelPeriod` candles to compute a Donchian-like close-price channel used for flat filters and orientation checks.
* Detects five-bar fractal highs and lows directly from the candle stream.
* Feeds the built-in `WilliamsPercentRange` indicator to monitor optional exit signals.

## Trading workflow
1. **Fractal detection** – the strategy tracks five consecutive finished candles. When the middle bar's high/low is the extreme compared to its two predecessors and two followers, it registers an upper or lower fractal and marks a pending short or long signal.
2. **Signal ageing** – every new candle increases the fractal age. If `SignalAgeLimit` bars pass without execution, the pending signal expires.
3. **Channel evaluation** – the rolling close channel supplies three filters:
   - *Fractal tunnel*: when `UseFractalChannel` is enabled, the close price must stay within a percentage of the distance between the latest fractal high and low (`DepthPercent`).
   - *High/Low orientation*: with `UseHighLowChannel`, the close must penetrate only a limited portion of the envelope (`OrientationPercent`).
   - *Flat blocking*: if `AllowFlatTrading` is disabled, trades are suspended while the channel width stays below `FlatThresholdPips`.
4. **Order execution** – once the filters pass, the strategy normalizes the desired `OrderVolume` against the instrument constraints and sends a market order in the fractal direction.
5. **Hedge stops** – when `UseCounterOrders` is active, the algorithm places the opposite stop order at the fractal price plus/minus `OffsetPips`, mirroring the EA's counter-trend staging.
6. **Williams exits** – if `UseWilliamsExit` is enabled, the most recent Williams %R value closes long positions when it rises above `-WilliamsThreshold` and short positions when it falls below `-100 + WilliamsThreshold`.

Stop loss and take profit distances are optional. Whenever `StopLossPips` or `TakeProfitPips` is positive, the strategy converts the pip distance into an absolute price offset using the instrument tick size (with the 3/5 digit adjustment from the EA) and delegates protective order management to `StartProtection`.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Base market order size before normalization against the instrument limits. |
| `StopLossPips` | `500` | Protective stop distance in pips. Converted to price and applied via `StartProtection`. |
| `TakeProfitPips` | `500` | Take profit distance in pips. Converted to price and applied via `StartProtection`. |
| `OffsetPips` | `5` | Extra distance used when staging counter-trend stop orders. |
| `ChannelPeriod` | `14` | Number of recent candles stored for the close-price channel. |
| `UseFractalChannel` | `false` | Requires price to remain inside the inner fractal corridor before entries. |
| `DepthPercent` | `25` | Percentage of the fractal range that defines the inner corridor. |
| `UseHighLowChannel` | `false` | Enables the Donchian-style close channel orientation filter. |
| `OrientationPercent` | `20` | Permitted penetration into the close channel when `UseHighLowChannel` is true. |
| `AllowFlatTrading` | `true` | Allows trading even when the market is flat according to the close channel width. |
| `FlatThresholdPips` | `20` | Minimum channel width (in pips) required when flat trading is disabled. |
| `UseWilliamsExit` | `false` | Activates Williams %R based exit rules. |
| `WilliamsPeriod` | `14` | Look-back period for the Williams %R indicator. |
| `WilliamsThreshold` | `30` | Sensitivity threshold (percentage points) for Williams %R exits. |
| `UseCounterOrders` | `false` | Places the opposite stop order after a market entry. |
| `SinglePosition` | `false` | Blocks additional entries in the same direction while a position is open. |
| `SignalAgeLimit` | `3` | Maximum number of new bars during which a fractal signal stays valid. |
| `CandleType` | `H1` | Candle data series used for analysis (defaults to one-hour time frame). |

## Usage notes
* The strategy expects instruments with a valid `PriceStep`, `MinVolume`, and `VolumeStep` so that volume normalization and pip conversion work correctly.
* Counter-trend orders are automatically cancelled when the position is closed, when the strategy stops, or when the feature is disabled.
* Williams %R exits act as a safety net and can close positions even if the original fractal signal is still active.
* The algorithm resets all cached state (fractal buffers, Williams history, staged orders) whenever `OnReseted` is triggered.

## Differences from the MetaTrader version
* The StockSharp implementation uses high-level `SubscribeCandles().Bind(...)` subscriptions instead of manual indicator loops.
* Protective stops rely on `StartProtection`, so no direct stop/limit order bookkeeping is required.
* Volume is normalized against exchange limits before orders are sent, matching StockSharp conventions.
