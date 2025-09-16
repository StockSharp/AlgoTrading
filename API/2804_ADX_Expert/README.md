# ADX Expert Strategy

## Overview
The **ADX Expert Strategy** is a direct conversion of the original MetaTrader 4 expert advisor "ADX Expert" (MQL script 20315). The expert looks for crosses between the positive and negative Directional Index (DI) lines while the Average Directional Index (ADX) remains below a specified threshold, indicating that the market is ranging. Only one position can be open at a time, just like in the source expert.

## Trading Logic
1. The strategy subscribes to the selected candle series (15-minute candles by default) and calculates the Average Directional Index with the configured period.
2. A buy order is placed when:
   - The +DI line crosses above the -DI line.
   - The ADX value stays below the defined threshold (default 20), signaling a weak trend.
   - The current spread is below the `MaxSpreadPoints` filter.
   - No position is currently open.
3. A sell order is placed when:
   - The +DI line crosses below the -DI line.
   - The ADX value is still lower than the allowed threshold.
   - The spread requirement and flat-position condition are satisfied.
4. Protective stop-loss and take-profit levels are assigned through `StartProtection`, mirroring the fixed stop and target from the MQL version. They are expressed in price points (price steps) and can be disabled by setting the values to zero.

The strategy relies on a single position workflow: new signals are ignored until the current position is closed by its protective orders.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size used for every market order. | 0.1 |
| `AdxPeriod` | Period for ADX calculation. | 14 |
| `AdxThreshold` | Maximum ADX value that still allows a trade. | 20 |
| `MaxSpreadPoints` | Maximum allowed spread in price points. Set to 0 to disable the filter. | 20 |
| `StopLossPoints` | Stop-loss distance in price points. | 200 |
| `TakeProfitPoints` | Take-profit distance in price points. | 400 |
| `CandleType` | Candle type for indicator calculations (15-minute candles by default). | 15-minute time frame |

## Additional Notes
- The spread filter requires order book updates to read best bid and ask prices. Ensure that your data provider supplies this information.
- All comments and logs are written in English for clarity, complying with repository guidelines.
- The strategy is intended for educational purposes. Test it thoroughly in a simulated environment before deploying it to live trading.
