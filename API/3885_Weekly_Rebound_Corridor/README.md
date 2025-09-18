# Weekly Rebound Corridor Strategy

## Overview
The Weekly Rebound Corridor strategy replicates the behavior of the MetaTrader 4 Expert Advisor `2_Otkat_Sys_v1_1`. The system searches for a strong gap between the previous session close and the open price that occurred 24 candles earlier. When the detected gap exceeds a configurable corridor threshold and it is the specified trading day of the week, the strategy enters the market during the first minutes of the new trading day. Protective stop-loss and take-profit levels are applied, and all open positions are force-closed shortly before the trading session ends.

## Trading Logic
1. **Data Preparation**
   - Uses minute candles by default. The candle type is configurable to accommodate other bar sizes.
   - Keeps track of the previous candle close and maintains a circular buffer that returns the open price observed 24 candles ago.
2. **Signal Generation**
   - On the specified trading day of the week (MetaTrader format: `0 = Sunday`, `6 = Saturday`), the strategy evaluates finished candles whose local time is between 00:00 and 00:03.
   - Calculates the difference between the historical open (24 candles ago) and the latest closed candle. If the difference exceeds the configured corridor threshold, a market order is sent:
     - **Long setup**: Historical open minus previous close is greater than the corridor threshold.
     - **Short setup**: Previous close minus historical open is greater than the corridor threshold.
   - Each trading day can trigger at most one entry.
3. **Trade Management**
   - Stop-loss and take-profit levels are expressed in points. The tick size of the instrument converts the point values into actual price offsets.
   - Long trades add the original MT4 offset of three extra points to the take-profit distance.
   - The strategy continuously monitors candle highs and lows to detect stop-loss or take-profit hits and closes the open position with a market order when triggered.
   - Any remaining open position is closed after 22:45 local exchange time to emulate the end-of-day flat rule from the original Expert Advisor.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `TakeProfitPoints` | Take-profit distance in points. Long trades add three additional points, as defined in the MT4 script. | `5` |
| `StopLossPoints` | Stop-loss distance in points. | `49` |
| `TradeVolume` | Volume submitted with market orders. The value is automatically aligned with the instrument volume step. | `1` |
| `CorridorPoints` | Minimum required gap between the historical open and the most recent close. | `10` |
| `TradeDayOfWeek` | Trading day in MetaTrader numbering (`0 = Sunday` … `6 = Saturday`). | `5` (Friday) |
| `CandleType` | Candle data type used for analysis. | `1 minute` |

## Notes
- The strategy operates exclusively on completed candles to align with the project guidelines.
- Ensure that the selected instrument provides enough historical data to build the 24-candle buffer before expecting entries.
- The volume and point-based parameters should be adjusted to match the instrument specification (tick size, lot step, trading schedule).
