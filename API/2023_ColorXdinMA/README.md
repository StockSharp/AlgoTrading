# ColorXdinMA Strategy

## Overview
ColorXdinMA Strategy implements the XdinMA indicator, calculated as `ma_main * 2 - ma_plus`, where both components are simple moving averages with different lengths. The strategy monitors the slope of this line and opens positions when the slope changes direction.

## Trading Logic
- When the indicator was declining and turns upward, a long position is opened. Existing short positions are closed.
- When the indicator was rising and turns downward, a short position is opened. Existing long positions are closed.

Only completed candles are processed. Orders are placed using market executions.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `MainLength` | Period of the main moving average. | 10 |
| `PlusLength` | Period of the additional moving average. | 20 |
| `CandleType` | Timeframe of candles used for calculation. | 6 hours |

## Notes
This implementation is a high-level StockSharp strategy and can be extended with risk management or visualization features as needed.
