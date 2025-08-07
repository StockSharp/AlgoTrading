# Tendency EMA + RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy layers a fast/medium EMA crossover on top of a slower trend EMA
and an RSI filter. Long trades require the fast EMA to cross above the medium
EMA while both remain above the slow trend line and the candle closes bullish.
Short trades mirror these rules. RSI extremes close positions, and an optional
"close after X bars" feature locks in profits if price moves in the expected
direction quickly.

The approach aims to participate only in pullback entries that align with the
prevailing trend, using the RSI to exit when momentum becomes overstretched. It
works best on intraday charts where EMA crossovers offer timely signals and
multiple setups occur each session.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above medium EMA, both above slow EMA, bullish candle.
  - Fast EMA crosses below medium EMA, both below slow EMA, bearish candle.
- **Long/Short**: Long enabled, short optional.
- **Exit Criteria**:
  - RSI > 70 closes long; RSI < 30 closes short.
  - Optional: close after X bars if trade is profitable.
- **Stops**: None built‑in.
- **Default Values**:
  - RSI length = 14.
  - EMA A/B/C lengths = 9/21/50.
  - Close after X bars = off, X = 5.
- **Filters**:
  - Category: Trend + Momentum
  - Direction: Both (long default)
  - Indicators: EMA, RSI
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Short
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
