# MA2 CCI
[Русский](README_ru.md) | [中文](README_cn.md)

Dual exponential moving average crossover strategy confirmed by a Commodity Channel Index (CCI) zero-line break. Position size and stop placement are derived from Average True Range (ATR) volatility and a configurable risk percentage.

## Details

- **Data**: Time-based candles (default 1 hour) supplied by the selected `Candle Type` parameter.
- **Entry**: Go long when the fast EMA crosses above the slow EMA and CCI crosses above zero on the same bar; go short on the opposite crossover with CCI breaking below zero.
- **Exit**: Close longs when the fast EMA crosses back below the slow EMA or price touches the fixed stop; close shorts when the fast EMA crosses above the slow EMA or price hits the short stop.
- **Risk**: Stop distance equals the greater of ATR (length `AtrPeriod`) or `MinStopPoints` multiplied by the instrument price step. Trade size is the portfolio value times `RiskPercent`, divided by that stop distance.
- **Instruments**: Trend-following forex or index symbols that support hedging in the original MetaTrader version; also applicable to other assets with clear momentum swings.
- **Environment**: Designed for continuous session markets where EMA/CCI signals align with ATR-based risk controls.

## Parameters

- `CandleType` – Timeframe and data type used for calculations and order flow.
- `FastMaPeriod` – Period of the fast EMA (default 10).
- `SlowMaPeriod` – Period of the slow EMA (default 37).
- `CciPeriod` – Lookback of the CCI oscillator confirming momentum (default 39).
- `AtrPeriod` – ATR length used to estimate current volatility for stop placement (default 3).
- `RiskPercent` – Fraction of current portfolio equity risked per trade (default 2%).
- `MinStopPoints` – Minimum stop distance expressed in price steps to emulate the MetaTrader pip filter (default 15).

## Notes

- Works best when run on liquid pairs and indices where EMA/CCI crossings are reliable; thin markets can trigger premature exits.
- Because stops are recalculated only on entry, the strategy keeps the risk profile stable and mirrors the fixed stop-loss logic from the original MQL expert.
- Portfolio valuation must be provided by the connected account for the position sizing to operate; otherwise the engine falls back to the strategy `Volume` or instrument minimum volume.
