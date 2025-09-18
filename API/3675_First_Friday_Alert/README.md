# First Friday Alert Strategy

## Overview
The **First Friday Alert Strategy** is a direct conversion of the MetaTrader 4 expert advisor `FirstFriday.mq4`. The original algorithm monitors daily candles and prints a platform log message whenever the first Friday of a new month is detected. The StockSharp version preserves the same behaviour by subscribing to daily candles and producing an informational log entry when the condition is met.

## Trading Logic
1. Subscribe to daily candles for the configured security.
2. Track the open time of the most recently processed candle to avoid duplicate handling when historical data is replayed or resubmitted.
3. For each finished candle, determine whether its open time falls on a Friday and the calendar day is between 1 and 7.
4. When the check succeeds, emit an informational log message (`AddInfoLog`) with the exact calendar date of the detected candle.

No market orders are sent; the strategy only reports the event to the log, matching the behaviour of the source EA that simply printed a message to the MetaTrader journal.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Daily timeframe (`TimeSpan.FromDays(1).TimeFrame()`) | Defines which candles are observed for the first-Friday detection. Change the value if the broker provides custom daily bars or alternative periodicities. |

## Implementation Notes
- High-level StockSharp API (`SubscribeCandles().Bind(...)`) is used to process candle updates.
- A private field stores the last processed candle time to prevent duplicate notifications.
- All comments are written in English, as required by the project guidelines.
- The strategy overrides `GetWorkingSecurities()` so the environment knows that it needs daily candles for the selected security.

## Usage
1. Attach the strategy to any security that provides daily candles.
2. Start the strategy. Once the first Friday of a new month appears in the incoming data, the log will contain a message similar to `Detected the first Friday daily candle for Friday, 3 March 2023.`
3. Use the log output as a trigger for manual workflows or as an informational signal for further automation.

Because no trading instructions are issued, the strategy is safe to run in both simulation and live environments without risking unintended orders.
