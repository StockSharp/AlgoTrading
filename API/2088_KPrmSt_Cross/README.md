# KPrmSt Cross Strategy

## Overview
The KPrmSt Cross strategy is a port of the MetaTrader 5 expert `exp_kprmst.mq5`. It uses a stochastic-like oscillator known as KPrmSt to capture reversals when the oscillator's main line crosses the signal line.

The strategy subscribes to candles of configurable timeframe and calculates the `Stochastic` indicator (used as a KPrmSt approximation). When the %K line crosses below the %D line, it opens a long position; when %K crosses above %D, it opens a short position. Existing positions are reversed accordingly.

## Parameters
- `Candle Type` – timeframe of candles used for calculations.
- `K Period` – number of bars for calculating the main line.
- `D Period` – period for smoothing the signal line.
- `Slowing` – additional smoothing applied to %K.
- `Stop Loss` – protective loss in price units. Set to 0 to disable.
- `Take Profit` – target profit in price units. Set to 0 to disable.

## Trading Logic
1. The strategy listens for finished candles only.
2. The stochastic oscillator values are stored to detect crossovers.
3. When %K falls below %D after being above it, a long position is opened or the short position is closed.
4. When %K rises above %D after being below it, a short position is opened or the long position is closed.
5. Optional stop-loss and take-profit levels close the position when reached.

## Notes
- The KPrmSt indicator from the original expert is approximated by StockSharp's `Stochastic` indicator.
- Money management options from the original script are not implemented.
- The strategy requires market data feed and order routing supported by StockSharp.
