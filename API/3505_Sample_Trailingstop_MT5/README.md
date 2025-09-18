# SampleTrailingstop MT5 Strategy

## Overview
The **SampleTrailingstopMt5Strategy** reproduces the behaviour of the MetaTrader 5 expert advisor `SampleTrailingstop-MT5.mq5` using StockSharp's high level API. The strategy constantly maintains paired breakout stop orders, protects filled positions with dedicated exit orders and applies a trailing stop once the trade becomes profitable. All calculations rely on the instrument price step so that the logic matches the original "points"-based implementation.

## Trading logic
1. **Data feed**. The strategy subscribes to level 1 quotes to receive the best bid/ask prices that drive the order and trailing stop updates.
2. **Entry orders**.
   - A buy stop order is placed above the current market using `BuyStop`. The order is refreshed only when the previous instance completes.
   - A sell stop order mirrors the long entry using `SellStop` below the market.
   - Both entry orders share the same configurable volume, stop-loss and take-profit distances. Orders also receive an expiration time one day ahead, matching the MQL implementation.
3. **Position protection**.
   - After fills, the strategy tracks the net signed position and the average entry price.
   - Separate exit stop and take-profit orders are created (`SellStop`/`BuyStop` and `SellLimit`/`BuyLimit`) so that protective levels remain on the exchange even if the entry orders are cancelled or expire.
   - The exit orders are continuously synchronised with the current position size and the most recent average entry price.
4. **Trailing logic**.
   - When the floating profit reaches the configured trailing distance, the protective stop is tightened to maintain that distance from the current bid (for longs) or ask (for shorts).
   - The trailing stop never crosses the entry price and respects a minimum update increment equal to one price step.
5. **Position tracking**. Every own trade updates the cumulative position value and recalculates the weighted average entry price so partial fills and reversals are processed correctly.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `TradeVolume` | Fixed order volume (lots or contracts) used for both breakout stop orders. |
| `TakeProfitPoints` | Distance in instrument points for the profit target. Set to zero to disable the take-profit. |
| `StopLossPoints` | Distance in points for the protective stop-loss. |
| `TrailingStopPoints` | Trailing distance in points applied once the position is in profit. Zero disables trailing. |

## Behavioural notes
- Entry orders are only re-submitted after the previous instance finishes (filled, cancelled or expired). This mirrors the `CheckPendingOrder` logic from the original expert.
- The stop-loss and take-profit distances are always converted into price values using `Security.PriceStep`, ensuring consistent behaviour across different instruments.
- If the position is fully closed, the strategy automatically cancels all remaining exit orders and resets the internal averages.
- The strategy relies solely on level 1 data and does not require candles or indicators, keeping the conversion close to the MQL template.

## Usage
1. Assign the desired security and portfolio before starting the strategy.
2. Adjust the four public parameters to align with the traded instrument (volume, stop-loss, take-profit and trailing distance).
3. Launch the strategy. It will autonomously manage breakout orders and position protection in real time.
