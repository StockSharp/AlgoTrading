# Smart Trend Follower Strategy

## Overview
The **Smart Trend Follower Strategy** is a StockSharp port of the MetaTrader 5 expert advisor *Smart Trend Follower*. The
original system alternates between a contrarian moving-average crossover and a trend-following setup that uses stochastic
confirmation. It scales into positions with a martingale-like volume multiplier and maintains a shared take-profit/stop-loss
for each directional basket. The StockSharp version keeps the same behaviour while using the high-level API (candle
subscriptions, indicator bindings and market orders).

## Signal Logic
Two independent signal engines are available and can be switched with the `SignalMode` parameter:

1. **CrossMa** – replicates the original contrarian crossover. When the fast SMA crosses *below* the slow SMA (fast < slow
   but previously fast > slow) the strategy opens or averages long positions. When the fast SMA crosses *above* the slow
   SMA (fast > slow but previously fast < slow) it opens or averages shorts.
2. **Trend** – follows the original trend mode that requires confirmation from the stochastic oscillator. A bullish signal
   appears when the fast SMA stays above the slow SMA, the candle closes higher than it opened, and the stochastic %K value
   is at or below 30. A bearish signal requires fast < slow, a bearish candle body, and stochastic %K at or above 70.

Signals are evaluated on finished candles only. Whenever a new signal arrives while opposite positions are still open, the
strategy first liquidates the opposing basket and only then processes new entries to stay aligned with the direction of the
current signal.

## Position Scaling
The strategy reproduces the MQL martingale logic:

- The first order uses `InitialVolume` lots.
- Every additional averaging order multiplies the previous volume by `Multiplier` (values ≤ 1 disable volume growth).
- A new averaging order for the active direction is allowed only after the market moves by `LayerDistancePips` pips away
  from the best entry price of the current basket (lowest long fill or highest short fill).
- Volumes are normalised using the instrument `VolumeStep`, `VolumeMin` and `VolumeMax` limits when available.

## Risk Management
For each directional basket the strategy tracks a shared breakeven price (volume-weighted average of all fills):

- `TakeProfitPips` defines the distance between the average entry price and a basket take-profit. Long baskets exit when the
  candle high touches that level, short baskets when the candle low reaches it. Set to 0 to disable take-profit targets.
- `StopLossPips` mirrors the behaviour for protective exits. Long baskets close when the candle low breaks below the stop,
  short baskets when the candle high crosses above it. Set to 0 to disable the protective stop.

Exit orders are executed via market orders when the next finished candle confirms that the level has been reached. The
strategy maintains `_longExitRequested` and `_shortExitRequested` flags to avoid duplicated exit submissions while fills are
still pending.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SignalMode` | enum (`CrossMa`, `Trend`) | `CrossMa` | Selects the signal engine (contrarian crossover or trend with stochastic filter). |
| `CandleType` | `DataType` | 30-minute time frame | Primary candle series used for calculations and signal generation. |
| `InitialVolume` | decimal | `0.01` | Base order size in lots for the first entry of any basket. |
| `Multiplier` | decimal | `2` | Volume multiplier applied to each additional averaging order. |
| `LayerDistancePips` | decimal | `200` | Minimum pip distance from the best entry before adding another order in the same direction. |
| `FastPeriod` | int | `14` | Period of the fast simple moving average. |
| `SlowPeriod` | int | `28` | Period of the slow simple moving average (must be greater than `FastPeriod`). |
| `StochasticKPeriod` | int | `10` | Lookback length for the stochastic oscillator %K line. |
| `StochasticDPeriod` | int | `3` | Smoothing length for the stochastic %D line. |
| `StochasticSlowing` | int | `3` | Additional smoothing applied to %K before %D calculation. |
| `TakeProfitPips` | decimal | `500` | Distance in pips from the average entry where the basket take-profit is placed. Set 0 to disable. |
| `StopLossPips` | decimal | `0` | Protective stop distance in pips. Set 0 to disable the hard stop. |

## Implementation Notes
- Pip size is derived from the instrument `PriceStep` and `Decimals`, matching the MetaTrader notion of “point” (e.g.
  0.0001 for 5-digit FX quotes).
- Position tracking uses two lists of `PositionEntry` objects to mirror MetaTrader’s per-ticket accounting. Entries are
  reduced FIFO-style when opposite trades close part of a basket.
- All indicator calculations rely on StockSharp’s high-level binding API (`SubscribeCandles().BindEx(...)`). No manual calls
  to `GetValue` are required and indicators are never injected into `Strategy.Indicators`.
- The strategy calls `StartProtection()` on start, allowing StockSharp to manage global risk-control modules (break-even,
  margin checks, etc.).
- Because StockSharp consolidates positions net-by-direction, opposite positions are fully closed before new entries are
  evaluated. This keeps the implementation deterministic and closely aligned with the original EA behaviour.

## Files
- `CS/SmartTrendFollowerStrategy.cs` – C# implementation of the strategy using the StockSharp high-level API.

