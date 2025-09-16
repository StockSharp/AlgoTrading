# MA Crossover Multi Timeframe Strategy

This strategy reproduces the idea of the original **MA Crossover** expert advisor for MetaTrader 4. It compares two moving averages that may come from different timeframes. A bullish crossover (fast MA above slow MA) opens a long position, while a bearish crossover opens a short position. Optional filters control the permitted trade direction, the active trading schedule and an equity guard. Internal stop-loss, take-profit and trailing logic emulate the "hidden" exits from the MQL version.

## Trading logic

1. Subscribe to two candle streams (current and previous timeframes) and calculate the selected type of moving averages.
2. Apply the configured bar shifts to the moving average values before comparing them.
3. Ignore unfinished candles and wait for both moving averages to be formed.
4. Skip trading outside of the configured day/time window or when the equity guard is triggered.
5. On a bullish crossover:
   - Optionally close a short position if `ClosePositionsOnCross = true`.
   - Open a long position if long trading is allowed.
6. On a bearish crossover:
   - Optionally close a long position if `ClosePositionsOnCross = true`.
   - Open a short position if short trading is allowed.
7. Manage the open position with stop-loss, take-profit and trailing rules expressed as percentages of the entry price.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `AllowedDirection` | Trade direction filter (`LongOnly`, `ShortOnly`, `LongAndShort`). |
| `ClosePositionsOnCross` | Close the opposite position when a crossover appears before opening a new trade. |
| `MaType` | Moving average calculation type (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `CurrentMaPeriod` | Period for the fast moving average. |
| `PreviousPeriodAddition` | Extra length added to the slow moving average (`PreviousMaPeriod = CurrentMaPeriod + addition`). |
| `CurrentShift` / `PreviousShift` | Number of completed bars used to shift the moving average values backward. |
| `CurrentCandleType` / `PreviousCandleType` | Candle data for the fast and slow moving averages. |
| `StopLossPercent` | Stop-loss distance in percent of the entry price (hidden exit). |
| `TrailingStopPercent` | Trailing stop distance in percent based on the best achieved price. |
| `TakeProfitPercent` | Take-profit distance in percent of the entry price (hidden exit). |
| `StartDay` / `EndDay` | Day-of-week filter for trading activity. |
| `StartTime` / `EndTime` | Intraday time window for opening new trades. |
| `ClosePositionsOnMinEquity` | Close all positions when the equity guard is triggered. |
| `MinimumEquityPercent` | Minimum percentage of the initial portfolio value allowed by the equity guard. |

## Risk management

- The strategy calculates stop-loss, take-profit and trailing levels internally and exits via market orders, mimicking the hidden protective logic of the MQL script.
- `MinimumEquityPercent` stores the initial portfolio value at start-up and can trigger a forced flat if equity drops below the threshold.
- Position size is controlled through the base `Strategy.Volume` property. The default volume is set to `1`.

## Usage notes

- The strategy requires candle data for both configured timeframes. Ensure that the associated connectors support the requested timeframes.
- When both moving averages use the same timeframe the strategy still subscribes to two streams to keep the logic symmetrical.
- Because stop and take-profit exits are executed via market orders, no protective orders remain in the order book.
- The parameters map to the main inputs of the original MQL expert advisor, while risk/margin management features that depend on broker-specific functions (hedging, averaging) are intentionally omitted.

## Differences from the MQL version

- Averaging features (`Average_Up`, `Average_Down`) and hedging settings are not implemented to keep the logic compatible with the high-level StockSharp API.
- The equity guard uses the portfolio value from StockSharp instead of free-margin specific calculations.
- Risk exits are executed through market orders on candle close events and are therefore always hidden from the order book.

