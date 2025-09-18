# Breadandbutter2 Strategy

## Overview
The Breadandbutter2 strategy is a direct conversion of the MT4 expert advisor from `MQL/7710/Breadandbutter2.mq4`. The system monitors one-hour candles and tracks three linear weighted moving averages (LWMA) built on the candle open prices. A synchronized crossover of the three averages indicates a trend reversal. The strategy immediately flips the position to align with the new direction and optionally pyramids additional orders while the trend persists.

## Core Logic
1. Subscribe to one-hour candles (configurable via **Candle Type**).
2. Calculate LWMA(5), LWMA(10), and LWMA(15) on candle opens.
3. Detect a bullish reversal when the previous candle had `LWMA5 < LWMA10 < LWMA15` and the current candle shows `LWMA5 > LWMA10 > LWMA15`. Detect a bearish reversal with the opposite inequality sequence.
4. On a bullish crossover, target a long position of **Volume** lots. On a bearish crossover, target an equally sized short position. The strategy adjusts the existing position by buying or selling only the difference between the current and target exposures.
5. After each entry the **Interval** counter resets. Once **Interval** finished candles pass without a new crossover, the strategy adds another order in the current direction (pyramiding) and refreshes protective orders.
6. Profit target and loss limit are attached to every resulting position using **Take Profit** and **Stop Loss** distances expressed in price steps. Setting either value to zero disables the corresponding protection.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| **Volume** | 0.1 | Order size in lots for each base entry and pyramid layer. |
| **Take Profit** | 20 | Distance in price steps for the take-profit order. Set to 0 to disable. |
| **Stop Loss** | 20 | Distance in price steps for the protective stop. Set to 0 to disable. |
| **Interval** | 4 | Number of finished candles to wait before adding another pyramid position. Zero disables pyramiding. |
| **Cross Filter** | 1.1 | Reserved parameter kept from the original code for future ADX filtering (currently not used). |
| **Candle Type** | 1-hour time frame | Candle data source for the LWMA calculations. |

## Position Management
- The helper method `AdjustPosition` ensures the final position exactly matches the desired exposure after every crossover.
- Pyramiding trades rely on the current sign of `Position` to add lots in the existing direction only.
- `SetTakeProfit` and `SetStopLoss` are invoked after each trade to keep risk controls in sync with the latest position size.

## Notes
- The MT4 script calculated an ADX value but never used it; the **Cross Filter** parameter is retained for compatibility and future extension.
- The original MQL implementation had the interval counter commented out. The StockSharp version activates the intended pyramiding behaviour by counting finished candles.
- `StartProtection()` is called during `OnStarted` to activate built-in position protection services.
