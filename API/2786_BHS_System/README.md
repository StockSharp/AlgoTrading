# BHS System Strategy

## Overview

The BHS System is a breakout-style approach that converts the original MetaTrader 5 expert advisor into the StockSharp high-level API. The strategy observes the relationship between price and a Kaufman Adaptive Moving Average (AMA). When the current bar closes above the AMA, the system prepares to join a bullish breakout; when the close falls below the AMA, it prepares for a bearish expansion. Instead of entering immediately, the algorithm waits for price to touch predefined “round number” levels and submits stop orders at those levels. This keeps the behaviour of the ported strategy identical to the MQL version where pending orders were always aligned to rounded price boundaries.

## Trading Logic

1. On every finished candle the strategy calculates the next higher and next lower round-price levels. Rounding uses the user-defined step (in points) and the instrument price step to produce exact exchange-compatible trigger prices.
2. The previous AMA value (shifted by one bar, as in the original MQL implementation) is compared with the current candle close.
3. If there is no open position and no active entry order:
   - When close &gt; AMA, a buy stop is placed at the rounded ceiling level.
   - When close &lt; AMA, a sell stop is placed at the rounded floor level.
4. Pending orders automatically expire after the configured number of hours. This mirrors the life-time field of the MT5 order request.
5. When an entry order executes, the opposite pending order is cancelled and a protective stop order is registered using the selected stop-loss distance. The system then monitors price movement and moves the stop according to the trailing parameters.
6. Trailing stops are only adjusted when price has advanced by at least the trailing distance plus the trailing step. This avoids constant modifications and mirrors the discrete trailing logic in the MT5 code.

## Risk Management

- **Initial stop-loss:** Separate point-based distances for long and short trades are converted into absolute price offsets and used to place protective stop orders immediately after entry.
- **Trailing stop:** Long and short positions have independent trailing distances. Stops are updated only when the new stop improves by at least the trailing step, preventing micro-adjustments in quiet markets.
- **Order expiration:** Both entry orders store their creation time. If the order remains active after the specified number of hours, it is cancelled to avoid stale pending exposure.

## Parameters

- `OrderVolume` – lot size used for both entries and protective orders.
- `StopLossBuyPoints` / `StopLossSellPoints` – stop-loss distance in points for long and short positions respectively.
- `TrailingStopBuyPoints` / `TrailingStopSellPoints` – trailing stop distance for long and short positions expressed in points.
- `TrailingStepPoints` – additional gap (in points) required before the trailing stop can be improved again.
- `RoundStepPoints` – number of points used when constructing rounded trigger levels.
- `ExpirationHours` – life span of a pending entry order. When set to zero, orders never expire automatically.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – parameters of the Kaufman Adaptive Moving Average used as the directional filter.
- `CandleType` – data type/timeframe of candles that drive the strategy.

## Implementation Notes

- The strategy uses StockSharp’s `KaufmanAdaptiveMovingAverage` indicator and a file-scoped namespace consistent with repository guidelines.
- All trading operations rely on high-level API helpers (`BuyStop`, `SellStop`, `CancelOrder`) and no indicator values are retrieved through `GetValue` calls.
- Chart support is enabled: the subscription draws candles, the AMA line, and own trades when a charting context is available.
- Protective logic is consolidated in a single stop order reference so the trailing mechanism reuses the original stop instead of spawning additional orders.
- The conversion keeps comments in English and preserves the behaviour of the original MQL trailing routine by using the same threshold checks.
