# Auto KD Crossover Strategy

## Overview
The Auto KD Crossover strategy replicates the original MQL5 `autoKD_EA` example.  
It uses the `StochasticOscillator` indicator to generate buy and sell signals based on crossovers of the %K and %D lines.

The base calculation uses the RSV formula:
`RSV = (Close - LowestLow) / (HighestHigh - LowestLow) * 100`
where the highest high and lowest low are computed over `KDPeriod` bars.  
The %K line is a moving average of RSV with length `KPeriod`; %D is a moving average of %K with length `DPeriod`.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `KDPeriod` | Number of bars for the RSV base period. | 30 |
| `KPeriod` | Smoothing period for the %K line. | 3 |
| `DPeriod` | Smoothing period for the %D line. | 6 |
| `CandleType` | Type and timeframe of candles used for calculations. | 5 minute time frame |
| `Volume` | Order volume inherited from `Strategy`. | `Strategy.Volume` |

All parameters are available for optimization.

## Trading Logic
1. Subscribe to the selected candle series and compute the Stochastic oscillator.
2. When the previous value of %K was below %D and the current %K crosses above %D, a long position is opened.
3. When the previous value of %K was above %D and the current %K crosses below %D, a short position is opened.
4. The strategy maintains only one position at a time. Crosses in the opposite direction close the position and open the opposite side.
5. `StartProtection()` enables default loss/profit protection mechanisms provided by StockSharp.

## Visualization
The strategy automatically displays candles, the Stochastic indicator, and executed trades on the chart.

## Notes
- Works on any instrument and timeframe.
- Parameters should be adapted to the volatility of the selected market.
