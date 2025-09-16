# Stochastic Histogram Strategy

This strategy is a StockSharp port of the original MQL expert `Exp_Stochastic_Histogram`.
It uses the Stochastic oscillator to produce contrarian trading signals in two modes:

- **Levels** – a signal appears when %K exits the overbought or oversold areas defined by `HighLevel` and `LowLevel`.
- **Cross** – a signal appears when %K crosses the %D line. The trade is opened in the opposite direction of the crossover.

Whenever a new signal is received, the strategy closes an existing position and opens a new one in the required direction.

## Parameters

- `KPeriod` – main %K period.
- `DPeriod` – %D smoothing period.
- `Slowing` – additional smoothing of %K.
- `HighLevel` – upper threshold for the Levels mode.
- `LowLevel` – lower threshold for the Levels mode.
- `Mode` – either Levels or Cross.
- `CandleType` – candle timeframe used for calculations.

## How it works

For every finished candle the Stochastic oscillator is updated and evaluated. In **Levels** mode a long trade is opened when %K returns below the high level and a short trade when %K rises above the low level. In **Cross** mode a long trade is opened on downward crossovers of %K below %D, while upward crossovers trigger short trades. The strategy always has at most one open position.
