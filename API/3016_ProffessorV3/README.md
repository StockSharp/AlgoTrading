# Proffessor v3 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview

This strategy is a full conversion of the MetaTrader expert *Proffessor v3* into the
StockSharp high level API. It keeps the original concept of combining ADX regime
filtering with a grid of protective and averaging orders.

- **Indicator**: 14-period Average Directional Index (ADX) with +DI/-DI values.
- **Modes**: flat regime (ADX below threshold) and trending regime (ADX above
  threshold).
- **Orders**: opens one market position and surrounds price with pending orders
  to hedge, pyramid or mean-revert.
- **Exit**: closes every position and pending order when the configured profit or
  loss level is reached.
- **Schedule**: trades only inside the selected hour range.

## Trading Logic

### Regime detection
1. Subscribe to the configured candle type and calculate ADX values.
2. Delay the ADX signal by the configured number of closed candles (`BarOffset`)
   to replicate the original use of `CopyBuffer(handle, shift)`.
3. When no position is open, evaluate the latest delayed ADX values:
   - *Flat bullish*: `ADX < AdxFlatLevel` and `+DI > -DI`.
   - *Flat bearish*: `ADX < AdxFlatLevel` and `+DI < -DI`.
   - *Trending bullish*: `ADX ≥ AdxFlatLevel` and `+DI > -DI`.
   - *Trending bearish*: `ADX ≥ AdxFlatLevel` and `+DI < -DI`.

### Order placement
For every mode the strategy opens a market position with the base volume and
then places a symmetric grid around the current price. Grid distances are
expressed in "points" exactly as in the MQL code and automatically scaled by the
instrument price step.

- **Flat bullish**: long market entry, protective sell-stop below bid, buy limits
  below the ask and sell limits above the bid to capture oscillations.
- **Flat bearish**: short market entry, protective buy-stop above ask, buy limits
  on pullbacks and sell limits higher to reload shorts.
- **Trending bullish**: long market entry, sell-stops for hedging and buy-stops
  for breakout pyramiding.
- **Trending bearish**: short market entry, sell-stops to trail the trend and
  buy-stops to cap reversals.

Grid spacing is calculated with the same formula as the original: each level
adds `GridStep + GridDeltaIncrement * level / 2`. Volume for every pending order
is adjusted with `LotMultiplier` and `LotAddition`, then normalized to the
exchange volume step and limits.

### Exit management
- The unrealized profit is calculated from the strategy position average price
  and the latest candle close.
- If the profit exceeds `ProfitTarget` or drops below `LossLimit` (when the
  latter is non-zero), the strategy closes the net position and cancels all
  pending orders.
- Trading is skipped outside the `[StartHour, EndHour)` interval, matching the
  original `Time()` helper.

## Implementation Notes

- Bid/ask prices for pending orders are approximated from the last candle close
  plus/minus half of the price step. This mirrors the tick-based logic in a
  candle-driven environment.
- Point values are scaled by the symbol price step and adjusted for three- and
  five-digit quotes exactly like the MQL `m_adjusted_point` variable.
- Volume and price normalization respects the symbol step, minimum and maximum
  constraints before sending any order.
- The strategy processes only finished candles to avoid premature signals.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Base market order volume. |
| `LotMultiplier` | Multiplier applied to every pending order volume. |
| `LotAddition` | Extra volume added to pending orders after the multiplier. |
| `MaxLevels` | Maximum number of grid levels per side. |
| `GridDeltaIncrement` | Increment added to grid spacing as levels deepen (points). |
| `GridInitialOffset` | Distance to the first protective order (points). |
| `GridStep` | Base distance between consecutive levels (points). |
| `ProfitTarget` | Unrealized profit level that triggers closing everything. |
| `LossLimit` | Unrealized loss level that triggers closing everything (0 disables). |
| `AdxFlatLevel` | ADX threshold separating flat and trending regimes. |
| `BarOffset` | Number of closed candles used to delay ADX values. |
| `StartHour` | Hour when the trading window opens (UTC). |
| `EndHour` | Hour when the trading window closes (UTC). |
| `CandleType` | Candle series used for calculations. |

