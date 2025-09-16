# X Trader Strategy

This strategy implements a contrarian moving average cross system originally written in MQL as **X trader**.
It uses two simple moving averages and opens positions opposite to the direction of the cross. Risk is managed
using fixed take‑profit and stop‑loss in absolute points via `StartProtection`.

## How it works

1. Subscribe to candle data of the specified time frame.
2. Calculate two moving averages with configurable periods.
3. Track the last two values of each average to detect a cross.
4. When the fast average crosses above the slow average and remains above for two bars while two bars ago it was below,
   a **short** position is opened.
5. When the fast average crosses below the slow average and remains below for two bars while two bars ago it was above,
   a **long** position is opened.
6. Only one position may be open at a time. Protection automatically exits trades when price moves by the
   configured take‑profit or stop‑loss amount.

## Parameters

- `CandleType` – candle series to use.
- `Ma1Period` – period of the first moving average.
- `Ma2Period` – period of the second moving average.
- `TakeProfitPoints` – profit target in price points.
- `StopLossPoints` – loss limit in price points.

## Indicator

- `SimpleMovingAverage` – used twice with different periods.

## Risk Management

`StartProtection` is enabled in `OnStarted` and applies the take‑profit and stop‑loss values to all positions.
