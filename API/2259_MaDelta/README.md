# MaDelta Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MaDelta strategy measures the difference between a fast and a slow moving average. The difference is scaled by a multiplier and raised to the third power, producing an oscillating value `px`. Two dynamic thresholds separated by `Delta` (in pips) track the recent high and low of this value. When `px` breaks above the upper threshold, the strategy switches to a long bias; when `px` falls below the lower threshold, it switches to a short bias. Existing positions opposite to the new bias are closed and a new trade is opened in the direction of the signal.

The approach effectively captures momentum bursts when the distance between the two moving averages expands rapidly. Cubing the difference exaggerates strong moves while filtering small fluctuations. The `Delta` parameter defines how far `px` must travel before a reversal is recognized, preventing whipsaw in flat markets.

## Details

- **Entry Criteria**:
  - **Long**: `px > hi` sets `trade = 1` and opens a long when no position exists.
  - **Short**: `px < lo` sets `trade = -1` and opens a short when flat.
- **Reverse Logic**:
  - Long signal while short closes the short with a market buy before entering long.
  - Short signal while long closes the long with a market sell before entering short.
- **Indicators**:
  - Fast moving average (SMA) with period `FastMaPeriod`.
  - Slow moving average (EMA) with period `SlowMaPeriod`.
  - Oscillator: `px = ((Multiplier * 0.1) * (FastMA - SlowMA))^3`.
- **Parameters**:
  - `Delta` – size of the high/low channel in pips.
  - `Multiplier` – scales the MA difference before cubing.
  - `FastMaPeriod` – length of the fast SMA.
  - `SlowMaPeriod` – length of the slow EMA.
  - `Volume` – order volume for entries.
  - `CandleType` – timeframe of processed candles.
- **Other Notes**:
  - Works only with finished candles.
  - No explicit stop-loss or take-profit; positions reverse on opposite signals.
  - Uses high-level API with indicator binding and automatic charting.
