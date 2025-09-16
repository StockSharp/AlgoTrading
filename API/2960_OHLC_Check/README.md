# OHLC Check Strategy

## Overview
The OHLC Check Strategy replicates the classic MetaTrader expert advisor that inspects the open, high, low, and close structure of the previous candle. The strategy evaluates the candle body on a configurable historical offset and opens a new position in the direction of that body while optionally mirroring the signal. It is designed for simple price-action driven execution without relying on oscillators or moving averages.

## Trading Logic
1. The strategy subscribes to the configured candle series and waits for the bar to finish before processing.
2. For each finished candle the engine stores the open and close price so the user-defined shift ("SignalShift") can reference older bars.
3. A bullish body (close above open) generates a long signal, while a bearish body (close below open) generates a short signal. If the body is flat no trade is created.
4. The `ReverseSignals` flag can invert the trade direction, reproducing the reverse-trading mode from the original expert advisor.
5. When there is no active position, the strategy attempts to open a market order in the detected direction as long as the current spread is within the allowed `SpreadLimitPips` threshold. The spread is monitored using the order book feed.
6. When a position already exists the opposite signal triggers a position close instead of a full reversal, matching the MQL logic.
7. Optional stop-loss and take-profit protections are launched at start-up using pip distances converted to the instrument price step, recreating the MQL money-management behaviour.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5-minute time frame | Data series used for OHLC evaluation. |
| `StopLossPips` | 50 | Stop-loss distance measured in pips; `0` disables the stop. |
| `TakeProfitPips` | 100 | Take-profit distance measured in pips; `0` disables the target. |
| `ReverseSignals` | `false` | Inverts the direction of long and short signals. |
| `SpreadLimitPips` | 1 | Maximum spread, in pips, allowed when opening a new position. |
| `SignalShift` | 1 | Number of completed candles back used for signal calculation (1 = previous candle). |
| `OrderVolume` | 1 | Volume sent with each market order. |

## Usage Notes
- The strategy uses the instrument metadata to convert pip values into price-step distances. Instruments with 3 or 5 decimal places automatically receive the standard ten-point pip adjustment.
- The order book subscription should be enabled in the data feed so that spread checks work correctly. If no bid/ask quotes are available the strategy will skip opening new trades.
- Protective stops are initiated once during `OnStarted`. Changing stop parameters afterwards requires restarting the strategy to apply new protections.
- Because the strategy reacts only to the candle body, high and low values are ignored exactly as in the original MetaTrader version.

## Deployment Steps
1. Attach the strategy to an instrument that supplies both candles and order book quotes.
2. Configure the parameters according to the desired trading style (time frame, pip distances, and volume).
3. Launch the strategy. It will wait for the next completed candle before performing any action.
4. Monitor the log for spread rejections or executed trades, and adjust parameters as needed.
