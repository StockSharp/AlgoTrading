# Gold Dust Strategy

## Overview

The Gold Dust strategy reproduces the MetaTrader 5 expert advisor "Gold Dust" inside the StockSharp framework. It evaluates up to two perceptrons built from a linear weighted moving average (LWMA) applied to the weighted candle price. Each perceptron observes how the price deviates from the moving average at four different lookback points spaced by the MA period. When the perceptron output is positive the original expert opens a sell position, and when it is negative it opens a buy position. The StockSharp port keeps the same behaviour while relying on the high-level candle subscription API.

## Signal generation

1. Subscribe to the configured `CandleType` and compute a `WeightedMovingAverage` with the period taken from `MaPeriod`.
2. On every finished candle store the candle's open and close prices together with the LWMA value. The strategy always keeps three full MA periods of history to mirror the `CopyRates`/`CopyBuffer` calls from the MQL version.
3. Calculate the price/MA offsets:
   - `a1` – current close minus current LWMA
   - `a2` – open price one MA period ago minus LWMA at the same candle
   - `a3` – open price two MA periods ago minus LWMA at the same candle
   - `a4` – open price three MA periods ago minus LWMA at the same candle
4. Build the perceptron output `result = Σ (wi × ai)` where each weight is the raw parameter (e.g. `X11`) minus 100, matching the original `w = x - 100` transformation.
5. Interpret the perceptron outputs depending on `PassMode`:
   - `1` – use the first perceptron only.
   - `2` – use the second perceptron only.
   - `3` – require both perceptrons to produce the same non-zero sign.
6. A negative signal opens or maintains a long position, a positive signal opens or maintains a short position, and a zero signal triggers profit-taking on existing positions.

## Position management

- **Entries** – the strategy trades with a fixed `TradeVolume`. Entering long closes any outstanding short exposure and vice versa so that only one directional position remains, matching the behaviour of `m_need_open_buy`/`m_need_open_sell` in the original code.
- **Stop-loss** – `StopLossPips` is converted into absolute price distance using `Security.PriceStep`. For instruments quoted with three or five decimals the distance is multiplied by ten to mimic the "adjusted point" logic in the MQL version. The stop is evaluated on every completed candle: if the candle's low (for longs) or high (for shorts) crosses the stop level the position is closed with a market order.
- **Trailing stop** – when `TrailingStopPips` is greater than zero the trailing logic becomes active. After the price moves by `TrailingStopPips + TrailingStepPips` in the trade's favour the stop is stepped to `close ± TrailingStopPips` (depending on direction). Trailing is candle-based and creates a stop even if the initial stop-loss was disabled, just like `PositionModify` in the EA.
- **Profit management** – when no perceptron agrees on a direction (`signal == 0`) the strategy closes the position only if the floating profit is positive. This reproduces `CloseProfitPositions` where swaps, commissions, and profit must be greater than zero.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Base volume for every new entry. Opposite positions are flattened before taking a new side. |
| `StopLossPips` | `150` | Initial stop-loss distance in adjusted pips (takes the 3/5-digit multiplier into account). Set to zero to disable the initial stop. |
| `TrailingStopPips` | `25` | Trailing stop distance in adjusted pips. Set to zero to disable trailing. |
| `TrailingStepPips` | `5` | Additional favourable move (in pips) required before the trailing stop is advanced. |
| `MaPeriod` | `20` | Period length of the weighted moving average that feeds the perceptrons. |
| `CandleType` | `H1` | Candle series used for signal evaluation. Any other timeframe supported by the data provider can be selected. |
| `PassMode` | `1` | Controls which perceptron(s) are evaluated: 1 – first, 2 – second, 3 – consensus of both. |
| `X11`, `X21`, `X31`, `X41` | `100` | Raw weights for perceptron #1. The strategy subtracts 100 from each value before using it in the perceptron. |
| `X12`, `X22`, `X32`, `X42` | `100` | Raw weights for perceptron #2, handled the same way as the first set. |

## Notes on the conversion

- The original EA relied on tick-by-tick updates to manage stops; the StockSharp port evaluates stops and trailing on candle close. This keeps the implementation within the high-level API while remaining faithful to the overall logic.
- Money-management via `CMoneyFixedMargin` was replaced with a fixed `TradeVolume` parameter. Users can integrate their own position-sizing logic if required.
- Perceptron calculations avoid direct indicator buffers (`CopyBuffer`) by caching the necessary candle and MA values in bounded lists.
- All pip distances respect the MetaTrader "adjusted point" convention: if the security trades with 3 or 5 decimals the distance is multiplied by ten before being applied to price levels.

## Usage tips

1. Create or select a symbol, then set `CandleType` to the timeframe that corresponds to the historical chart used in the MQL version.
2. Review the perceptron weights (`X**`) and `PassMode` to match the optimised configuration from MetaTrader. Each weight can be optimised independently inside StockSharp.
3. Adjust `TradeVolume` so that it complies with the connected broker's minimum and step size. The strategy automatically adds the absolute opposite exposure when flipping direction.
4. Monitor the log: every time the trailing stop is advanced or a stop-loss is triggered a message is recorded, which helps verify that the port behaves like the original EA.

