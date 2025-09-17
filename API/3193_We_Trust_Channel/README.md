# WE TRUST Channel Strategy

## Overview
The **WE TRUST Channel Strategy** is a high-level StockSharp port of the MetaTrader 5 expert advisor "WE TRUST". The system trades pullbacks toward a linear weighted moving average that is surrounded by standard deviation bands. When price closes outside the bands the strategy anticipates mean reversion and opens a market position back toward the middle of the channel. Signal reversal, optional closing of opposite trades, and pip-based money management parameters mirror the original expert.

## Trading Logic
1. Subscribe to the configured candle type (hourly candles by default) and calculate two indicators on the selected price source:
   - A linear weighted moving average (**LWMA**) with configurable period and shift.
   - A standard deviation envelope with its own period and shift.
2. Convert pip-based offsets into absolute price distances using the instrument `PriceStep`. Five-digit and three-digit quotes multiply the step by 10 to emulate the MetaTrader definition of a pip.
3. Compute the upper and lower channel limits: `LWMA ± StdDev ± ChannelIndentPips` (converted into price units).
4. Evaluate finished candles only. When the chosen candle price closes below the lower channel the strategy generates a **buy** signal. When it closes above the upper channel it generates a **sell** signal.
5. Optionally invert the signals when **ReverseSignals** is enabled. Optionally flatten an opposite position before acting on a new signal when **CloseOpposite** is enabled.
6. Submit market orders with the configured volume whenever the current position is flat or aligned with the signal direction.

## Risk Management
- **StopLossPips** and **TakeProfitPips** translate pip distances into absolute protective orders via `StartProtection`. Set them to `0` to disable the respective level.
- **TrailingStopPips** and **TrailingStepPips** control a pip-based trailing stop that follows profitable trades. Both parameters convert into price distances using the same pip size logic.
- All exits are performed with market orders to stay close to the MQL5 implementation.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Trade volume submitted with each market order. | `0.1` |
| `StopLossPips` | Stop-loss distance expressed in pips (0 disables the stop). | `40` |
| `TakeProfitPips` | Take-profit distance expressed in pips (0 disables the target). | `60` |
| `TrailingStopPips` | Trailing stop distance in pips. | `10` |
| `TrailingStepPips` | Trailing step in pips between stop adjustments. | `10` |
| `MaPeriod` | Period of the linear weighted moving average. | `60` |
| `MaShift` | Number of bars that the moving average is shifted forward. | `0` |
| `StdDevPeriod` | Period of the standard deviation calculation. | `50` |
| `StdDevShift` | Number of bars that the deviation value is shifted. | `0` |
| `SignalBarOffset` | Number of completed bars to look back when evaluating signals. | `1` |
| `ChannelIndentPips` | Additional buffer added outside the deviation bands. | `1` |
| `ReverseSignals` | Invert the buy/sell logic of the channel breakout. | `false` |
| `CloseOpposite` | Close an opposite position before entering a new trade. | `false` |
| `AppliedPrice` | Candle price component fed into both indicators. | `Weighted` |
| `CandleType` | Candle data type requested from the connector. | `1 hour` time frame |

## Notes
- The strategy relies on valid `PriceStep` metadata. If the exchange does not provide it the code falls back to `Security.Step` and finally to `1`.
- Only the C# implementation is included in this directory. The Python port is intentionally omitted per instructions.
- The logic processes finished candles only and does not attempt to accumulate partial bar data.
