# MartingailExpert v1.0 Stochastic Strategy (C#)

## Overview

The **MartingailExpert v1.0 Stochastic Strategy** is a direct conversion of the MetaTrader 4 expert advisor
`MartingailExpert_v1_0_Stochastic.mq4`. The strategy watches the %K/%D lines of the Stochastic Oscillator
and opens a position when the previous completed bar produces a momentum confirmation above (for longs)
or below (for shorts) configurable threshold zones. Once the first trade is live, the algorithm builds a
martingale ladder of additional market orders whose volume grows geometrically and whose shared take-profit
remains anchored to the price of the most recent addition.

The conversion relies entirely on StockSharp's high-level API: candle subscriptions, indicator binding, and
built-in `BuyMarket`/`SellMarket` helpers. All code comments were rewritten in English and the implementation
follows the tab-based indentation style required by the project guidelines.

## Trading Logic

### 1. Entry signal

1. The Stochastic Oscillator (`Length = KPeriod`, `%K` smoothing = `Slowing`, `%D` smoothing = `DPeriod`) is
   bound to the main candle subscription. Only finished candles are processed.
2. The strategy mimics the original MQL call `iStochastic(..., shift = 1)` by storing the previous bar values
   of %K and %D. A long entry is triggered when `K_prev > D_prev` and `D_prev > ZoneBuy`. A short entry is
   triggered when `K_prev < D_prev` and `D_prev < ZoneSell`.
3. The very first trade uses `BuyVolume` or `SellVolume` and resets any opposite direction state to avoid
   mixing long and short ladders.

### 2. Martingale averaging

1. Whenever there is an open cluster (`_buyOrderCount` or `_sellOrderCount` greater than zero) the strategy
   monitors the candle's low (for longs) or high (for shorts).
2. **Step calculation**
   * `StepMode = 0`: the next addition waits for the price to move by exactly `StepPoints × PointSize` against
     the latest filled order.
   * `StepMode = 1`: the distance becomes `StepPoints + max(0, 2 × ordersCount − 2)` points, matching the
     MQL expression `step + OrdersTotal*2 - 2`. The expression is multiplied by the instrument's point size
     (derived from `Security.PriceStep` and adjusted for 3/5 decimal FX quotes).
3. If the candle violates the trigger level, the strategy sends an immediate market order whose volume equals
   `previousVolume × Multiplier`. Volumes are normalized to the instrument's `VolumeStep`, capped by
   `VolumeMax` (when available) and rounded down to zero if they fall below `VolumeMin`.
4. After each addition, the shared target price is updated to
   `lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount` depending on the direction.

### 3. Take-profit management

1. The cluster is closed once the candle touches the shared target price (`High >= target` for longs,
   `Low <= target` for shorts). An additional check estimates the price-distance profit using the weighted
   average entry price to mirror the original `OrderProfit()` safeguard from MQL.
2. All open orders are flattened with a single `SellMarket(Math.Abs(Position))` or
   `BuyMarket(Math.Abs(Position))` call. After a successful exit the internal martingale state is reset.
3. If the external environment closes positions (manual intervention, stop-outs) the next candle with
   `Position == 0` automatically clears the cached martingale state, keeping the strategy consistent.

### 4. Additional implementation notes

* The point size is derived from `Security.PriceStep`. For 3- or 5-decimal FX symbols the value is multiplied
  by ten to emulate the MetaTrader concept of a pip (`Point`).
* `StartProtection()` is invoked once in `OnStarted` so the platform can attach common protective behaviours
  (timeouts, heartbeat, etc.).
* The strategy draws candles, the stochastic indicator, and own trades on a dedicated chart area for easier
  visual inspection during backtests.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `StepPoints` | decimal | `25` | Distance in points before another martingale order is placed. |
| `StepMode` | int | `0` | `0` – fixed distance, `1` – fixed plus `2 × ordersCount − 2` points. |
| `ProfitFactorPoints` | decimal | `10` | Points added (or subtracted) per open order to compute the cluster take profit. |
| `Multiplier` | decimal | `1.5` | Multiplier applied to the last order volume for the next addition. |
| `BuyVolume` | decimal | `0.01` | Volume of the initial long order. |
| `SellVolume` | decimal | `0.01` | Volume of the initial short order. |
| `KPeriod` | int | `200` | Lookback period of the stochastic oscillator. |
| `DPeriod` | int | `20` | Smoothing period for the %D signal line. |
| `Slowing` | int | `20` | Additional smoothing applied to %K (MetaTrader's `slowing`). |
| `ZoneBuy` | decimal | `50` | Minimum %D value required to allow long entries. |
| `ZoneSell` | decimal | `50` | Maximum %D value required to allow short entries. |
| `CandleType` | `DataType` | `5m time frame` | Candle type used for all indicator calculations. |

## Folder Structure

```
API/3991/
├── CS/
│   └── MartingailExpertV10StochasticStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```

Python implementation is intentionally omitted in accordance with the task requirements.
