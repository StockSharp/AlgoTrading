# Backbone Strategy (StockSharp)

## Overview
The **Backbone Strategy** ports the original MetaTrader 4 "Backbone.mq4" expert advisor to the StockSharp high level API. The system collects bid/ask extremes to determine the initial trading direction and then alternates between long and short baskets. Each basket is built gradually, adding one market order per completed candle until either the configured `MaxTrades` count is reached or protective orders close the position. Risk control is retained through a fractional risk model that scales the trade volume by the account value and stop-loss distance.

## Market Data Flow
- **Candles (`CandleType`)** – completed candles pace decision making; only one order can be issued per finished bar, exactly as in the MT4 script.
- **Order book snapshots** – best bid and ask values are tracked to reproduce trailing-stop calculations and the initial "extreme" discovery logic.
- **Strategy state** – the StockSharp `Strategy` base keeps the running position, average entry price, and PnL used to manage protective orders.

## Trading Logic
1. **Initial calibration** – while no direction is defined, the strategy records the highest bid and lowest ask seen. When price retraces by `TrailingStopPoints * PriceStep` from those extremes, the first basket direction is chosen.
2. **Order sequencing** –
   - If the last completed trade was short (`_lastPositionDirection == -1`) and there are no open trades, a new **buy market** order is submitted.
   - If the previous trade was long (`_lastPositionDirection == 1`) and the basket still has capacity, additional buy orders are submitted on subsequent candles.
   - Symmetric rules apply for sell orders when the last trade was long.
3. **Volume sizing** – every new order calls the MT4-inspired `Vol()` analogue. The available account value (current value → balance → starting balance) is multiplied by `MaxRisk` and divided by the stop-loss distance converted to money using `PriceStepCost`. The result is aligned with `VolumeStep`, bounded by `MinVolume`/`MaxVolume`, and rejected if it drops below the minimum trade size.
4. **Protective orders** – once a trade executes the strategy places a single stop-loss and take-profit order that covers the entire basket. Distances are expressed in "points" (price steps) just like the MQL version.
5. **Trailing stop** – when both `StopLossPoints` and `TrailingStopPoints` are positive, the stop order is reissued to lock in profits whenever price moves more than the trailing distance beyond the recorded entry price. Long baskets use the best bid as reference; short baskets use the best ask.
6. **Basket completion** – if either the stop-loss or take-profit order executes, all internal counters reset, leaving `LastPosition` unchanged so the next candle starts a basket in the opposite direction, mirroring the original EA behaviour.

## Money Management
- Uses the same fractional formula `1 / (MaxTrades / MaxRisk - openTrades)` as the MQL expert.
- Risk capital is estimated from `Portfolio.CurrentValue`, falling back to `CurrentBalance` or `BeginBalance`.
- Volume is discarded if the computed size is below the instrument's `MinVolume` after alignment to `VolumeStep`.
- Stop-loss and take-profit orders are recreated whenever volume changes so that protection always covers the entire basket.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `CandleType` | 15-minute time frame | Candle interval used to trigger new decisions. |
| `MaxRisk` | 0.5 | Fraction of the portfolio considered when sizing the next order. Must be positive. |
| `MaxTrades` | 10 | Maximum number of trades that can be accumulated in the current basket. |
| `TakeProfitPoints` | 170 | Take-profit distance measured in price steps. Set to `0` to disable. |
| `StopLossPoints` | 40 | Stop-loss distance measured in price steps. Required for trailing and position sizing. |
| `TrailingStopPoints` | 300 | Trailing-stop distance in price steps. Set to `0` to keep a static stop. |

## Conversion Notes
- The original EA modifies each order individually; the StockSharp version manages one aggregated stop-loss and take-profit per basket because StockSharp positions are netted by default.
- Volume sizing relies on `Security.PriceStepCost`. If the connector does not provide this value, the strategy falls back to the configured `Volume` property.
- Trailing updates are applied when a new candle arrives, matching the "once per bar" behaviour of the MT4 script (which only acted when `Bars > PrevBars`).
- The alternating logic keeps the last executed direction in `_lastPositionDirection`, so once a basket is closed the next candle automatically opens a basket in the opposite direction, just like the source code.
- Only the C# implementation is provided; there is no Python port in this directory.

## Usage Tips
- Assign instruments with accurate `PriceStep`, `PriceStepCost`, and volume metadata to obtain realistic position sizes.
- When backtesting, ensure the order book feed is available so that trailing-stop logic can access best bid/ask values.
- To disable aggressive scaling, increase `MaxTrades` or reduce `MaxRisk` so the `Vol()` replacement returns smaller volumes.
