# RPM5 BullsBearsEyes Strategy

## Overview
The **RPM5 BullsBearsEyes Strategy** is a C# port of the MetaTrader 4 expert *Rpm5_mt4v1*. The advisor rebuilt the custom BullsBearsEyes oscillator from Bulls Power and Bears Power readings and opened a single position that followed the prevailing bias. This StockSharp version reproduces the same behaviour using the high-level API while keeping the original risk parameters, trailing logic, and signal thresholds.

## Indicator reconstruction
- Two classic oscillators – **Bulls Power** and **Bears Power** – are calculated on the configured candle series.
- Their sum is passed through the identical four-stage IIR smoother used by the MT4 indicator. The smoothing factor (`Gamma`) controls how fast the oscillator reacts.
- The filtered output is transformed into a value between **0** and **1**. Values above the central threshold signal bullish dominance, values below it point to bearish control. Exact zero or one appear when either side is completely exhausted, matching the original indicator edge cases.

## Trading rules
1. The strategy subscribes to the selected timeframe (5 minutes by default) and waits for completed candles only.
2. When flat, it evaluates the BullsBearsEyes ratio:
   - **Long entry** – current value strictly above the `Threshold` (default 0.5).
   - **Short entry** – current value strictly below the `Threshold`.
   - The algorithm keeps at most one open position. Opposite signals are ignored until the active position is fully closed by risk management.
3. Once in a trade, the position is left untouched until a stop-loss, take-profit or trailing stop event occurs.

## Risk management
- **Stop-loss / take-profit** distances are recreated from the original 25 / 150 pip settings. They are recomputed using the instrument `PriceStep` (pip) each time a new position is opened.
- **ATR trailing**: on every finished candle the Average True Range (period `AtrPeriod`, default 5) is evaluated. The trailing distance equals one pip plus `AtrMultiplier × ATR`. When the close advances beyond that distance, the protective stop is tightened to maintain the gap, identical to the MQL logic that repeatedly called `OrderModify`.
- Protective levels are checked before processing new signals, ensuring that exits are always prioritised over fresh entries just like in the source EA.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `Bulls/Bears Period` | 13 | Averaging period for the Bulls Power and Bears Power indicators. |
| `Gamma` | 0.5 | Four-stage IIR smoothing ratio for the BullsBearsEyes oscillator. |
| `Threshold` | 0.5 | Divider between bullish (> threshold) and bearish (< threshold) zones. |
| `ATR Period` | 5 | Lookback used for the ATR-based trailing stop. |
| `ATR Multiplier` | 1.5 | Multiplier applied to ATR when deriving the trailing distance. |
| `Stop Loss (pips)` | 25 | Protective stop distance, converted from pips to price. |
| `Take Profit (pips)` | 150 | Profit target distance, converted from pips to price. |
| `Trade Volume` | 1 | Market order volume used for every new position. |
| `Candle Type` | 5 minute candles | Timeframe processed by the strategy. |

## Notes
- The port does not draw the visual daily channel objects that were present in MT4 because they were cosmetic only.
- All comments inside the code are written in English as requested.
- Tests are unchanged; run the existing solution level checks if validation is required.
