# Fractal AMA MBK Crossover Strategy

## Overview
The Fractal AMA MBK Crossover strategy uses the **Fractal Adaptive Moving Average (FRAMA)** together with an **Exponential Moving Average (EMA)** trigger line. Trading signals are generated when the FRAMA line crosses the EMA line.

## How It Works
- FRAMA adapts its smoothing factor based on the fractal dimension of recent price movement.
- The EMA acts as a trigger line that smooths price data.
- **Long entry:** when FRAMA crosses above the EMA and no long position is open.
- **Short entry:** when FRAMA crosses below the EMA and no short position is open.
- Existing positions can be protected with optional stop-loss and take-profit levels.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle type and timeframe used for calculations (default: 4-hour candles). |
| `FramaPeriod` | Period length for the FRAMA indicator. |
| `SignalPeriod` | Period length for the EMA trigger line. |
| `StopLoss` | Stop-loss distance from entry price in absolute price units (0 disables). |
| `TakeProfit` | Take-profit distance from entry price in absolute price units (0 disables). |
| `Volume` | Trade volume in lots. |

## Notes
- Only completed candles are processed.
- Trades are executed using market orders (`BuyMarket`/`SellMarket`).
- `FramaPeriod` and `SignalPeriod` parameters support optimization.
