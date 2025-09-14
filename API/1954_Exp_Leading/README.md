# Exp Leading Strategy

This strategy implements a crossover system based on the custom **Leading** indicator described by John F. Ehlers in *Cybernetics Analysis for Stock and Futures*. The indicator calculates two lines:

1. **NetLead** – smoothed leading filter controlled by the `Alpha1` and `Alpha2` coefficients.
2. **EMA** – a simple exponential moving average with a constant factor of 0.5.

The strategy operates on finished candles from the selected timeframe. When the NetLead line crosses **below** the EMA line, an upward reversal is anticipated and a long position is opened. Conversely, when NetLead crosses **above** the EMA line, a short position is opened. The previous position, if any, is closed implicitly when an opposite order is sent.

## Parameters

- `Alpha1` – coefficient for the intermediate leading calculation. Default: `0.25`.
- `Alpha2` – smoothing factor applied to the leading result. Default: `0.33`.
- `CandleType` – candle data type used for calculations. Default: 4‑hour timeframe.
- `StopLoss` – stop loss in absolute price units. Default: `1000`.
- `TakeProfit` – take profit in absolute price units. Default: `2000`.

## Trading Logic

1. Each finished candle updates the NetLead and EMA values.
2. If the previous bar showed NetLead above EMA and the latest bar shows NetLead below EMA, a **buy** market order is sent.
3. If the previous bar showed NetLead below EMA and the latest bar shows NetLead above EMA, a **sell** market order is sent.
4. `StartProtection` is used to automatically apply stop‑loss and take‑profit rules.

This example is intended for educational purposes to demonstrate how a MetaTrader strategy can be ported to the StockSharp high‑level API.
