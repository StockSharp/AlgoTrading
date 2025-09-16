# X Trader V2 Strategy

## Overview
This strategy is a contrarian moving average crossover system converted from the original MQL4 expert **X_trader_v2**. It uses two moving averages to detect sudden reversals and executes trades opposite to the crossing direction.

## How It Works
1. Two simple moving averages are calculated on the selected timeframe.
2. When the fast MA crosses **above** the slow MA, the strategy opens a **short** position.
3. When the fast MA crosses **below** the slow MA, the strategy opens a **long** position.
4. Only one position can be open at a time. A new trade is placed only after the previous one is closed and a fresh signal appears.
5. Built-in protection automatically places stop-loss and take-profit orders.

## Parameters
- `Ma1Period` – period of the fast moving average.
- `Ma2Period` – period of the slow moving average.
- `TakeProfitTicks` – take-profit distance in price ticks.
- `StopLossTicks` – stop-loss distance in price ticks.
- `CandleType` – candle type used for calculations.

## Notes
- The strategy subscribes to candle data via the high-level API.
- Indicator values are processed through bindings without direct calls to `GetValue`.
- The algorithm stores previous indicator values internally to avoid heavy history lookups.
