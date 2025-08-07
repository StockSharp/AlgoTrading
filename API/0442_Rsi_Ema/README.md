# RSI + EMA Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This system pairs a classic Relative Strength Index (RSI) oscillator with a dual moving-average trend filter. The RSI provides short-term overbought and oversold readings while the two exponential moving averages (EMAs) define the broader trend. The strategy only takes trades in the direction of the fast EMA relative to the slow EMA, helping avoid counter‑trend setups during strong directional moves.

When price momentum pushes RSI below the oversold threshold and the fast EMA is above the slow EMA, the market is assumed to be in an uptrend and a long position is opened. Conversely, if RSI rises above the overbought level while the fast EMA still exceeds the slow EMA, the strategy initiates a short trade, expecting a short‑term pullback inside the larger trend channel.

Positions are exited when RSI leaves the extreme zone on the opposite side, signalling that the mean reversion move has likely exhausted. The method is simple yet effective for capturing brief momentum swings in trending environments. It works well on liquid instruments where RSI extremes occur frequently but trend direction remains intact.

## Details

- **Entry Criteria**:
  - **Long**: `RSI < oversold` and `EMA1 > EMA2`.
  - **Short**: `RSI > overbought` and `EMA1 > EMA2`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `RSI > overbought`.
  - **Short**: `RSI < oversold`.
- **Stops**: None built-in.
- **Default Values**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
  - `EMA Lengths` = 150 / 600.
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
