# SMI Correct Strategy

## Overview
SMI Correct Strategy implements a trading system based on the Stochastic Momentum Index (SMI) indicator. The strategy watches the SMI line and its moving average signal line. A long position is opened when the SMI crosses below the signal line. A short position is opened when the SMI crosses above the signal line.

## Parameters
- **Candle Type** – time frame of candles used for calculations.
- **SMI Length** – number of periods for the Stochastic calculation.
- **Signal Length** – smoothing period for the signal line.

## How it works
1. The strategy subscribes to candles of the specified type.
2. For each finished candle, it updates the Stochastic oscillator and the signal moving average.
3. When the SMI crosses below the signal line, any short position is closed and a long position is opened.
4. When the SMI crosses above the signal line, any long position is closed and a short position is opened.

The sample also draws candles and indicator lines on a chart for visualization.
