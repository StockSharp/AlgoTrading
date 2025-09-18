# Binario 3 Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader 4 expert "Binario_3" from `MQL/7658/Binario_3.mq4`. The original EA surrounds the market with two 144-period exponential moving averages calculated on the candle highs and lows and trades the breakout of this adaptive channel. Pending stop orders are placed above the upper band and below the lower band, while protective stops, take profit targets and an optional trailing stop emulate the MetaTrader behaviour.

The StockSharp version keeps the same decision rules but is implemented with the high-level API:

1. Subscribes to the configured candle series and recalculates the two EMA envelopes every time a candle is completed.
2. When the latest close remains inside the channel, places buy stop and sell stop orders at the required offset from the EMA values.
3. Records the stop-loss and take-profit levels associated with each pending order so they can be applied to the position once an order is filled.
4. Tracks Level-1 quotes to manage open positions: closes trades if the price reaches the recorded stop-loss, the target or the trailing stop distance.
5. Cancels pending orders if price leaves the channel or if the opposite position becomes active, mirroring the clean-up logic in the MQL script.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `TakeProfit` | `850` points | Additional distance (in points) added to the breakout side when computing the take profit. |
| `TrailingStop` | `850` points | Distance in points used for trailing exits. Set to `0` to disable trailing. |
| `PipDifference` | `25` points | Offset from the EMA channel before placing pending orders. |
| `Lots` | `0.1` | Base trade volume used when risk-based sizing cannot be derived. |
| `MaximumRisk` | `10` | Risk multiplier copied from the original EA. The strategy estimates volume as `max(Lots, Balance * MaximumRisk / 50000)`. |
| `EmaPeriod` | `144` | Period of the exponential moving averages built on high and low prices. |
| `CandleType` | `1 hour` time frame | Candle series that drives indicator updates and order placement. |

All points are converted into actual price distances using the instrument's `PriceStep`. If the symbol does not expose a step the strategy falls back to `1`.

## Trading Logic

1. **Indicator calculations** – Two `ExponentialMovingAverage` instances process the candle high and low prices. Orders are generated only after both averages are fully formed.
2. **Pending orders** – When the close price lies inside the channel, buy stop and sell stop orders are placed at:
   - Buy stop: EMA(high) + spread + `PipDifference` * step.
   - Sell stop: EMA(low) - `PipDifference` * step.
   The stop-loss and take-profit values associated with those orders are stored until the position becomes active.
3. **Position management** – As soon as a position opens the strategy cancels the opposite pending order and adopts the stored stop/target levels. Level-1 quotes are monitored to close the trade if the market hits the stop-loss, take-profit or the trailing stop distance (`TrailingStop` * step).
4. **Trailing stop** – For long positions the trailing level follows the best bid once profit exceeds the configured distance; for shorts the level follows the best ask. The trailing level only moves in the direction of the trade, reproducing the MetaTrader trailing behaviour.
5. **Order clean-up** – When the latest close leaves the EMA channel both pending orders are cancelled to avoid unwanted entries, matching the original script's safety checks.

## Differences from the MQL Version

- The original EA modified server-side stop orders with `OrderModify`; the StockSharp port simulates the same effect by observing Level-1 quotes and calling `ClosePosition()` when a stop or target is reached.
- Trailing stops are implemented entirely inside the strategy because high-level StockSharp orders do not support on-exchange trailing instructions.
- Volume calculation uses the portfolio balance (`Portfolio.CurrentValue` or `Portfolio.BeginValue`) when available. If the balance is not known the strategy falls back to the configured `Lots` value.
- Prices are normalised to the instrument's price step before registering orders to keep them aligned with exchange requirements.

## Usage Notes

- Enable Level-1 subscriptions when running the strategy so that trailing stops and protective exits can react to live bid/ask updates.
- The strategy relies on completed candles. If the selected time frame is too large the response time will mirror that slower rhythm.
- Trailing can be disabled by setting `TrailingStop` to `0`. In this mode only the fixed stop-loss and take-profit levels are used.
