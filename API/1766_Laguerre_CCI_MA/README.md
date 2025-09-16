# Laguerre CCI MA

Strategy combining Laguerre filter, Commodity Channel Index (CCI) and an exponential moving average.

## Overview
- Laguerre filter highlights overbought and oversold extremes on a 0-1 scale.
- CCI confirms momentum in the same direction.
- EMA slope filters trades to the prevailing trend.

## Entry Rules
- **Long** when Laguerre value is 0, EMA is rising and CCI is below the negative `CciLevel` threshold.
- **Short** when Laguerre value is 1, EMA is falling and CCI is above the positive `CciLevel` threshold.

## Exit Rules
- Close long positions when Laguerre exceeds 0.9.
- Close short positions when Laguerre drops below 0.1.

## Parameters
- `LagGamma` – gamma value for the Laguerre filter.
- `CciPeriod` – period for CCI calculation.
- `CciLevel` – absolute CCI level used for entries.
- `MaPeriod` – period for the moving average.
- `TakeProfit` – take profit in absolute price units (optional).
- `StopLoss` – stop loss in absolute price units (optional).
- `CandleType` – candle type used for indicators.

The strategy processes only finished candles and uses StockSharp's high level API bindings for indicators.
