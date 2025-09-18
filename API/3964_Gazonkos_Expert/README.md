# Gazonkos Expert Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader 4 expert advisor "gazonkos expert" that was designed for the EUR/USD H1 chart. The EA waits for a strong one-hour momentum move, then enters in the direction of that move after a configurable pullback. Protective stop loss and take profit levels are applied as fixed distances measured in pips.

## Original MQL4 logic
- The EA continuously monitors the difference between two historical closes (`Close[t2] - Close[t1]`). The defaults are `t1 = 3` and `t2 = 2`, which correspond to the closes of the candles that finished two and three hours ago.
- A bullish impulse is detected when `Close[t2] - Close[t1]` exceeds `delta` points. A bearish impulse is detected when `Close[t1] - Close[t2]` exceeds the same threshold.
- Once an impulse is detected the EA records the highest (for bullish) or lowest (for bearish) bid that occurs before the next hour starts. If price retraces by `Otkat` points from that extreme within the same hour, a market order is sent in the impulse direction.
- Trades are blocked when there is already an open position with the same magic number or when a trade was already opened during the current hour.
- Every order is sent with a fixed take profit (`TakeProfit`) and stop loss (`StopLoss`) distance expressed in points.

## State machine in the C# version
The StockSharp implementation recreates the original state machine:
1. **WaitingForSlot** – verifies that no recent trade was opened in the current hour and that the configured maximum number of simultaneous trades has not been reached.
2. **WaitingForImpulse** – checks the historical closes to detect bullish or bearish impulses.
3. **MonitoringRetracement** – keeps track of the candle highs/lows after the impulse and waits for a pullback of `RetracementPips` (the former `Otkat` parameter) within the same hour.
4. **AwaitingExecution** – submits a market order in the impulse direction and immediately applies protective stop-loss and take-profit levels calculated from the instrument `PriceStep`.

The strategy only processes finished candles from the configured timeframe and ignores unfinished data, mirroring how the original EA evaluated conditions on closed hourly bars.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPips` | Distance between the entry price and the take profit level. |
| `RetracementPips` | Required pullback from the impulse extreme before entering. |
| `StopLossPips` | Distance between the entry price and the protective stop. |
| `T1Shift` | Index of the older reference close used for impulse detection (default 3). |
| `T2Shift` | Index of the newer reference close used for impulse detection (default 2). |
| `DeltaPips` | Minimum momentum distance that must separate the two reference closes. |
| `LotSize` | Fixed volume of every order. |
| `MaxActiveTrades` | Maximum number of simultaneous trades; values above one require that the broker account supports additive net positions. |
| `CandleType` | Timeframe of the candles used to evaluate the trading rules (default is 1 hour). |

All pip-based distances are converted to price offsets using `Security.PriceStep`. When the instrument has no price step information a default value of 0.0001 is used, matching the original EUR/USD configuration.

## Implementation notes
- The strategy works with StockSharp's high-level candle subscription API (`SubscribeCandles().Bind`).
- Closed prices are cached in a lightweight rolling buffer to emulate `Close[i]` lookups from the MQL4 version.
- After a trade is executed the strategy records the candle hour and blocks new entries until the next hour, reproducing the original `LastTradeTime` safeguard.
- `MaxActiveTrades` is interpreted against the current net position. On netting accounts this effectively limits the system to a single open trade, which matches the default behaviour of the MQL4 expert.
- Comments inside the code describe the C# state machine in English for easier maintenance.
