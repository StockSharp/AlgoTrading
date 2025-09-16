# Martin Martingale Strategy

## Overview
The strategy reproduces the behaviour of the original "Martin" Expert Advisor from MQL by running a hedged martingale grid around the current price. It continuously alternates long and short positions, doubling the traded volume on every reversal until the accumulated profit of the entire basket reaches the configured target. Candles are only used as a driver for the decision logic while actual executions rely on market and stop orders exposed by the StockSharp high level API.

## How It Works
1. At start-up the strategy reads the instrument `PriceStep` to convert the `EntryOffsetPoints` and `StepPoints` parameters into absolute price distances. If the price step is missing, a value of 1 is assumed.
2. Whenever there is no open position and no active martingale cycle, the strategy places a buy stop and a sell stop order around the latest close. The offsets are `EntryOffsetPoints * PriceStep`, which matches the 10 point distance used in the original MQL code.
3. When one of the stop orders fills, the opposite pending order is cancelled. The fill defines the first trade of the martingale sequence: the strategy stores its price, volume and direction, and sets the internal level counter to 1.
4. On each subsequent candle close the current close price is compared to the price of the last executed order. If the market has moved against that order by at least `martingaleLevel * StepPoints * PriceStep`, a market order is submitted in the opposite direction with doubled volume relative to the previous trade. The last trade information is updated after every execution.
5. The unrealised profit is evaluated as `PnL + Position * (closePrice - PositionPrice)`. When this aggregated profit exceeds the `ProfitTarget` parameter, the strategy sends `CloseAll()` to flatten every position in the basket, cancels all remaining orders and resets the cycle so that a fresh pair of stop orders can be placed.
6. The same reset also happens automatically when all positions are closed manually: the internal counters are cleared and new stop orders will be created on the next candle.

This workflow mirrors the alternating buy/sell logic of the original Expert Advisor while keeping the implementation fully within the high level StockSharp API.

## Parameters
- `StepPoints` – number of price steps used to calculate the reversal threshold for the next averaging order. Defaults to 10 and can be optimised.
- `EntryOffsetPoints` – offset for the initial buy/sell stop orders in price steps. Also defaults to 10 points like the MQL version.
- `ProfitTarget` – absolute currency profit required to close the whole martingale basket. Once the combined realised and unrealised PnL exceeds this value, all positions are liquidated.
- `CandleType` – candle subscription used to drive the strategy logic. The default is one-minute time frame, but any `DataType` supported by the venue can be selected.

The base trade size is taken from the strategy `Volume` property. Every new reversal multiplies this base by powers of two in the classic martingale manner.

## Practical Notes
- Always configure `Volume` to match the broker’s minimal lot size. The doubling scheme quickly increases exposure, so risk limits should be enforced externally.
- Because order placement is driven by candle closes, fast price moves within the candle can trigger entries slightly later than the tick-based MQL version. Nevertheless the stop orders keep the entry prices aligned with the original logic.
- The strategy draws price candles and own trades on the default chart area for easier visual tracking.
- No automated stop-loss is used. The only exit condition is the `ProfitTarget`, so the instrument and timeframe should be chosen carefully to control the risk of large adverse trends.

## Differences from the MQL Expert
- StockSharp uses net positions, therefore each reversal is executed with a market order that both closes the previous exposure and opens the new one in a single trade. The cumulative PnL of the basket remains identical to the hedged implementation.
- Tick-by-tick logic was replaced with candle closes for signal evaluation in order to stay within the recommended high level API usage.
- Order identifiers are tracked to avoid processing partial fills multiple times, ensuring the volume doubling logic remains consistent.

These changes keep the trading behaviour faithful to the source strategy while adapting it to the StockSharp framework.
