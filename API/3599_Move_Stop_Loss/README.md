# Move Stop Loss Strategy

## Overview
- Converts the MetaTrader 4 expert advisor **MoveStopLoss.mq4 (43793)** into the StockSharp framework.
- Focuses on managing already opened positions by dynamically trailing the stop loss.
- Supports two trailing modes: an automatic ATR-based mode and a fixed-distance manual mode.

## Trading Logic
1. Subscribes to the selected candle series and calculates the Average True Range (ATR) with the configured period.
2. Builds a rolling maximum of the last *AtrLookback* ATR values to replicate the MT4 search for the largest ATR from the previous 30 bars.
3. Determines the trailing distance:
   - If `AutoTrail` is enabled, multiplies the ATR maximum by `AtrMultiplier` (default 0.85) to obtain the trailing offset in price units.
   - If `AutoTrail` is disabled, converts `ManualDistance` (measured in price steps) to price units.
4. For a long position the strategy:
   - Detects a new entry and resets the stored stop order.
   - Waits until price moves above the stored entry price.
   - Places or moves a sell stop so that the stop remains `trailDistance` below the latest closing price.
5. For a short position the strategy mirrors the same rules, keeping the stop `trailDistance` above the price.
6. When no position is open the strategy cancels the protective stop and resets its state.

## Parameters
| Parameter | Description |
| --- | --- |
| `AutoTrail` | Enables ATR-based trailing distance. When disabled the strategy uses `ManualDistance`. |
| `ManualDistance` | Fixed trailing offset measured in security price steps. Active only when `AutoTrail` is false. |
| `AtrMultiplier` | Multiplier applied to the ATR maximum when `AutoTrail` is enabled. |
| `AtrPeriod` | Number of candles used in the ATR calculation. Matches the original 7-period ATR. |
| `AtrLookback` | Amount of historical ATR values included in the rolling maximum (default 30). |
| `CandleType` | Candle type (timeframe) processed by the trailing engine. |

## Implementation Notes
- The strategy does not open new positions; it only protects an existing net position with stop orders.
- Uses StockSharp's high-level API with `SubscribeCandles` and indicator `BindEx` to avoid manual data storage.
- `StartProtection()` is activated to ensure the strategy can cancel or move existing stop orders safely.
- All comments have been translated to English as requested.
