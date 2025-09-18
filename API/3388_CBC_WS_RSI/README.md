# CBC_WS_RSI Strategy

## Overview
The **CBC_WS_RSI Strategy** is a high-level StockSharp implementation of the MQL5 expert advisor that combines the "Three White Soldiers" and "Three Black Crows" candlestick patterns with RSI confirmation. The strategy focuses on identifying strong multi-candle reversals and only enters a trade when market momentum, measured by RSI, agrees with the pattern. Exits are controlled by RSI threshold crossovers and optional risk management through stop-loss and take-profit protections.

The strategy subscribes to a configurable candle series and processes data exclusively on fully formed candles. All logic is implemented using StockSharp's high-level API (`SubscribeCandles().Bind(...)`) without direct access to indicator buffers.

## Trading Logic
### Long Setup
1. Detects three consecutive bullish candles forming the **Three White Soldiers** pattern:
   - Each candle closes above its open.
   - Every close is higher than the previous close.
   - The second and third candle open inside the body of the previous candle.
2. Confirms that the RSI value of the current candle is **below or equal to the Long Confirmation level** (default 40).
3. If the account is flat, the strategy buys `Volume` lots at market. If a short position exists, it is covered before opening a new long position.

### Short Setup
1. Detects three consecutive bearish candles forming the **Three Black Crows** pattern:
   - Each candle closes below its open.
   - Every close is lower than the previous close.
   - The second and third candle open inside the body of the previous candle.
2. Confirms that the RSI value of the current candle is **above or equal to the Short Confirmation level** (default 60).
3. If the account is flat, the strategy sells `Volume` lots at market. If a long position exists, it is closed before opening a new short position.

### Exit Rules
- **Close Longs:** RSI crossing below either the Upper Exit level (default 70) or the Lower Exit level (default 30).
- **Close Shorts:** RSI crossing above either the Lower Exit level (default 30) or the Upper Exit level (default 70).
- **Protection:** Optional stop-loss and take-profit values can be defined as percentages of the entry price. When non-zero, they are managed via `StartProtection`.

All exit conditions use the most recent two RSI values to detect a level crossover, ensuring trades are closed as soon as momentum contradicts the active position.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Candle data type and timeframe to subscribe to. | 1-hour time frame |
| `RsiPeriod` | RSI period used for confirmation. | 37 |
| `LongConfirmationLevel` | Maximum RSI value that permits a long entry. | 40 |
| `ShortConfirmationLevel` | Minimum RSI value that permits a short entry. | 60 |
| `LowerExitLevel` | RSI level used to detect momentum reversal near oversold territory. | 30 |
| `UpperExitLevel` | RSI level used to detect momentum reversal near overbought territory. | 70 |
| `StopLossPercent` | Optional stop-loss in percent; 0 disables the protection. | 1 |
| `TakeProfitPercent` | Optional take-profit in percent; 0 disables the protection. | 2 |

All numeric parameters can be optimized via the built-in optimizer thanks to `SetCanOptimize(true)`.

## Visualization
When a chart area is available, the strategy draws:
- The selected candle series.
- The RSI indicator.
- Executed trades, making it easy to inspect pattern detections and exits.

## Usage Notes
- Ensure `Volume` is configured before starting the strategy.
- Works on any instrument that supports OHLC candle data.
- The pattern detection logic filters out doji-like candles by requiring non-zero candle bodies.
- RSI confirmations protect against false signals during weak reversals, keeping the strategy aligned with momentum.
