# EMA LWMA RSI Strategy

## Overview
The **EMA LWMA RSI Strategy** reproduces the MetaTrader expert advisor "EMA LWMA RSI" in StockSharp. It compares two moving averages that use the same applied price and optionally a forward shift, while a Relative Strength Index filter confirms the momentum. The algorithm only reacts to newly finished candles from the configured timeframe and trades a single net position: it closes any opposite exposure before opening a new order in the signalled direction. Stop-loss and take-profit distances are configured in pips and automatically scaled to the instrument's tick size.

## Trading Logic
1. Compute an exponential moving average (EMA) and a linear weighted moving average (LWMA) with individual lengths but the same applied price. If `MaShift` is greater than zero, both averages are shifted forward by the specified number of bars to mirror the MetaTrader "shift" argument.
2. Process an RSI with its own applied price. The strategy uses the classic 50 threshold to distinguish bullish and bearish momentum.
3. When a finished candle arrives:
   - A **buy** signal is generated if the EMA crosses **above** the LWMA (the previous EMA was greater than the previous LWMA, but the current EMA is below the current LWMA) and the RSI value is **above 50**.
   - A **sell** signal is generated if the EMA crosses **below** the LWMA (the previous EMA was lower than the previous LWMA, but the current EMA is above the current LWMA) and the RSI value is **below 50**.
4. Signals set internal pending flags. Before reversing, the strategy first closes the existing position with `ClosePosition()`. After the fill is confirmed, it immediately submits a market order in the requested direction. This mirrors the original expert advisor that waited for transaction confirmation before sending the next order.
5. Protective orders are started via `StartProtection`. If a stop-loss or take-profit is disabled (set to zero), that leg is omitted, matching the MQL behaviour.

## Implementation Notes
- Applied price selection supports the MetaTrader options (Close, Open, High, Low, Median, Typical, Weighted, Average). Weighted price is calculated as `(High + Low + 2 * Close) / 4`, identical to `PRICE_WEIGHTED`.
- Pip sizing automatically multiplies the instrument's `PriceStep` by 10 for 3/5-digit forex symbols, ensuring a pip equals 10 points on fractional quotes.
- Indicator bindings rely on StockSharp's high-level candle subscription. Shift handling uses `Shift` indicators instead of manual buffer indexing.
- The code keeps boolean flags for pending buy/sell requests. They prevent duplicate orders while the previous command is still pending and are cleared when fills arrive or when the position already matches the signal.
- Chart helpers draw both moving averages on the price pane and the RSI on a separate area for visual inspection.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `1h TimeFrame` | Candle series processed by the strategy. |
| `StopLossPips` | `int` | `150` | Stop-loss distance in pips. `0` disables the stop. |
| `TakeProfitPips` | `int` | `150` | Take-profit distance in pips. `0` disables the target. |
| `EmaPeriod` | `int` | `28` | Period of the exponential moving average. |
| `LwmaPeriod` | `int` | `8` | Period of the linear weighted moving average. |
| `MaShift` | `int` | `0` | Forward shift (bars) applied to both moving averages. |
| `RsiPeriod` | `int` | `14` | Averaging period of the RSI. |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Applied price forwarded to EMA and LWMA. |
| `RsiAppliedPrice` | `AppliedPriceType` | `Weighted` | Applied price used by the RSI. |

## Usage
1. Attach the strategy to the desired security and set the `CandleType` to match the timeframe used in MetaTrader.
2. Adjust pip-based protections and indicator settings if the broker uses different defaults.
3. Enable trading once the subscription is live. The strategy will manage one position at a time and use `ClosePosition()` before flipping direction.

No Python translation is provided for this strategy yet.
