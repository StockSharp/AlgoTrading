# ColorX2MA Digit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a port of the MQL5 expert **Exp_ColorX2MA_Digit**.
The original algorithm paints a double smoothed moving average in different colors depending on its slope and uses these colors to generate trading signals.
In this C# version the behavior is approximated by two simple moving averages and trades on their crossovers.

## Trading Logic

- A **fast** moving average smooths the price series.
- A **slow** moving average smooths the result of the fast one.
- When the fast average crosses above the slow average, the strategy opens a long position and closes any existing short position.
- When the fast average crosses below the slow average, the strategy opens a short position and closes any existing long position.
- Signals are processed only after the candle is finished.

## Parameters

- `FastLength` – length of the first smoothing (default 12).
- `SlowLength` – length of the second smoothing (default 5).
- `CandleType` – timeframe of candles used for calculations.

The strategy uses only high level API: `SubscribeCandles` with `Bind` to feed indicators and `BuyMarket`/`SellMarket` to manage positions. Comments in the code explain each step in English for easier maintenance.
