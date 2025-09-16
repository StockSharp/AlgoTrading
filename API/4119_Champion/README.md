# 4119 Champion Strategy

## Overview
This strategy is a high-level C# port of the MetaTrader Expert Advisor located in `MQL/919/champion.mq5`. The original EA waits for a Relative Strength Index (RSI) signal and places three stop orders in the direction of the anticipated breakout. Every pending order already includes a stop-loss and take-profit and the stop-loss is trailed whenever price moves favourably. The StockSharp version keeps the same behaviour while relying exclusively on high-level API calls (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, etc.).

The default configuration targets liquid FX instruments where the MetaTrader "point" matches the StockSharp `PriceStep` (typically 0.0001). The candle type is configurable and the strategy can be applied to any time frame as long as the account provides best bid/ask quotes and, optionally, stop level information.

## Strategy logic
1. **Signal generation**
   - An RSI of configurable length is calculated on completed candles.
   - The previous RSI value (one closed bar ago) is compared against a symmetric threshold (`RsiLevel`).
   - `RSI < RsiLevel` triggers a bullish setup; `RSI > 100 - RsiLevel` triggers a bearish setup.
2. **Pending order placement**
   - When there are no open positions and no active pending orders managed by the strategy, three identical stop orders are placed in the signalled direction.
   - Buy stops are placed above the best ask, sell stops below the best bid. The distance respects the server-provided stop level (if available) or the `MinOrderDistancePoints` fallback.
   - Order volume is calculated dynamically: available account value divided by `BalancePerLot`, clamped to the `[0.1, 15]` lot range and rounded to two decimals. Each pending order receives one third of the computed volume.
3. **Initial protective orders**
   - As soon as the first trade is filled, aggregated protective orders are registered: stop-loss at `entry ± StopLossPoints` and take-profit at `entry ± TakeProfitPoints` (MetaTrader points converted to price by `PriceStep`).
   - If `TakeProfitPoints` is zero the take-profit order is disabled.
4. **Trailing stop**
   - While a position is open the stop-loss order is tightened on every level-1 update.
   - For longs the new stop equals `max(entry + spread, bid - StopLoss)`; for shorts `min(entry - spread, ask + StopLoss)`.
   - Trailing is activated only when the move exceeds the sum of the broker stop level and the current spread, reproducing the original EA safeguards.
5. **Pending order maintenance**
   - Pending buy stops are moved closer to the market when their activation price is more than `RepriceDistancePoints` away from the current ask. The same logic applies to sell stops versus the current bid.
   - Repricing always honours the greater of `RepriceDistancePoints` and the effective stop level distance.
6. **Position exit**
   - Positions close via the protective stop-loss/take-profit orders or by manual user intervention. When the position size returns to zero the strategy cancels any remaining protective orders and waits for the next RSI signal.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TakeProfitPoints` | MetaTrader points added/subtracted from the fill price to place the take-profit order. Set to `0` to disable the target. |
| `StopLossPoints` | MetaTrader points added/subtracted from the fill price to place the stop-loss order and to compute the trailing distance. |
| `RsiPeriod` | RSI length (number of candles). |
| `RsiLevel` | Symmetric RSI threshold. Values below the level trigger longs, values above `100 - level` trigger shorts. |
| `BalancePerLot` | Account currency amount considered equivalent to one standard lot when sizing positions. |
| `MinOrderDistancePoints` | Fallback minimum distance (in points) between the market price and new stop orders when the trading venue does not report a stop level. |
| `RepriceDistancePoints` | Distance (in points) that triggers pending order repricing. |
| `CandleType` | Candle data type used for the RSI calculation. |

## Usage notes
- The strategy requires both candle data and level-1 quotes (best bid/ask). Without level-1 updates the trailing logic and pending-order maintenance are disabled.
- When the broker exposes a stop-level or stop-distance through level-1 metadata, it is automatically honoured. Otherwise configure `MinOrderDistancePoints` to match the instrument requirements.
- Position sizing falls back to the `Strategy.Volume` property whenever portfolio information is missing or the computed lot size becomes non-positive.
- Three pending orders are always placed together. Cancel unwanted orders manually if partial participation is required; the strategy will continue managing the remaining ones.

## Risk management
- Stop-loss and take-profit orders are native exchange/broker orders, mirroring the behaviour of the MetaTrader EA. When a position closes the protective orders are cancelled immediately.
- The trailing stop only moves in the direction of profit and never loosens the stop-loss. It activates once price has travelled at least `(StopLevel + spread)` beyond the entry price.
- Repricing logic prevents stale pending orders from being left behind after large jumps, reducing the probability of delayed fills.
