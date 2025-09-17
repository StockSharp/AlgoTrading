# Exp Fine Tuning MA Candle Strategy

## Overview
- Converted from the MetaTrader 5 expert `Exp_FineTuningMACandle.mq5` that trades on the colour of the *Fine Tuning MA Candle* indicator.
- Designed for StockSharp's high-level API: subscribes to a single candle series, derives indicator values through `BindEx`, and routes all orders via the `Strategy` helpers.
- Implements the same entry permissions and conditional closes as the original expert while respecting StockSharp's asynchronous execution model.

## Fine Tuning MA Candle indicator
- The indicator builds synthetic OHLC candles by applying a three-stage weighting scheme to the last `Length` candles of the price series.
  - `Rank1`, `Rank2` and `Rank3` control the curvature of the weighting ramps, while `Shift1`, `Shift2` and `Shift3` blend the ramps with a flat component.
  - The weighting is symmetric: the first half of the window is accelerated towards the centre, the second half decelerates away from it.
  - After normalisation the four weighted sums produce smoothed open, high, low and close prices.
- When the smoothed open and close differ by less than `GapPoints` (converted to the instrument's price step), the open is replaced with the previous synthetic close to remove price gaps.
- The candle is coloured **2** (bullish) when `Open < Close`, **0** (bearish) when `Open > Close`, and **1** when they are equal. Only the colour stream is used for trading decisions.
- `PriceShiftPoints` vertically offsets every synthetic candle by a configurable number of price steps.

## Trading rules
- Signals are produced on completed candles only. The strategy stores the indicator colours and evaluates the candle located `SignalBar` steps behind the latest finished one.
- **Bullish rotation (colour changes to 2):**
  - Existing short positions are closed if `SellPosClose` is enabled.
  - Once the position is flat, and if `BuyPosOpen` is allowed, a long market order for `Volume` lots is sent. If a short had to be closed first, the long entry is queued and fired as soon as the position returns to zero.
- **Bearish rotation (colour changes to 0):**
  - Existing long positions are closed if `BuyPosClose` is enabled.
  - Once flat, and if `SellPosOpen` is allowed, a short market order for `Volume` lots is sent. Pending entries are used in the same way as for long signals.
- Neutral colour (1) does not trigger any action.
- Orders are never stacked: the strategy opens at most one position at a time and waits for active positions to close before reversing.

## Risk management
- `StopLossPoints` and `TakeProfitPoints` represent distances in price steps. After a new position is filled the strategy registers protective stop and target orders using the actual fill price reported in `OnNewMyTrade`.
- Protective orders are cancelled automatically when the position returns to zero or whenever a new order is queued, mirroring the behaviour of the MQL helper functions.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle data type/timeframe used for indicator calculations. |
| `Length` | Number of candles processed by the indicator's weighted window. |
| `Rank1`, `Rank2`, `Rank3` | Power coefficients that shape the three weighting stages. |
| `Shift1`, `Shift2`, `Shift3` | Blend factors (0–1) that mix the weighting stages with a flat component. |
| `GapPoints` | Maximum difference between synthetic open and close that is suppressed by copying the previous close. Expressed in price steps. |
| `SignalBar` | How many closed candles to skip before reading the indicator colour. `1` means “use the latest completed candle”. |
| `BuyPosOpen` / `SellPosOpen` | Allow opening long/short positions. |
| `BuyPosClose` / `SellPosClose` | Allow closing long/short positions when the opposite colour appears. |
| `StopLossPoints` | Distance from the fill price to the protective stop. Set to `0` to disable. |
| `TakeProfitPoints` | Distance from the fill price to the profit target. Set to `0` to disable. |
| `PriceShiftPoints` | Vertical shift applied to the synthetic candles, expressed in price steps. |

## Implementation notes
- Uses `BindEx` because the custom indicator returns a complex value object that exposes the synthetic OHLC and colour simultaneously.
- Keeps only a small history of colour values (`SignalBar + 2` entries) to detect colour flips without storing large buffers.
- Entry reversals honour the asynchronous execution model by waiting for the position to flatten before sending the opposite side order, ensuring the behaviour matches the original expert's “close then open” workflow.
