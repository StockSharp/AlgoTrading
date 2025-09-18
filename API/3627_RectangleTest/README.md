# Rectangle Test Strategy

## Overview
The Rectangle Test strategy reproduces the MetaTrader "RectangleTest" expert using StockSharp's high-level API. It detects sideways ranges on an intraday time frame, checks whether two moving averages and the current price stay inside the detected range, and then trades breakouts away from the rectangle in the direction of the faster EMA. All logic is executed on completed candles received from a configurable candle source.

## Trading Logic
1. Subscribe to the primary candle stream (default: 1-hour time frame) and feed it into the following indicators:
   - **ExponentialMovingAverage (EMA)** with configurable length `EmaPeriod`.
   - **SimpleMovingAverage (SMA)** with configurable length `SmaPeriod`.
   - **Highest** and **Lowest** indicators with length `RangeCandles`, configured to read candle highs and lows. They provide the rectangle boundaries that emulate the MetaTrader array-based calculations.
2. Once all indicators are formed, compute the rectangle height in percent relative to the upper boundary. Only candles where the height is smaller than `RectangleSizePercent` are considered valid consolidations.
3. Require the EMA, SMA, and candle close to remain inside the rectangle. This reproduces the sideways filter from the MQL version.
4. **Short setup**:
   - EMA is above the SMA.
   - Close price is above the EMA (matching the "Ask > EMA" condition from MetaTrader on completed candles).
   - Optional liquidation of an existing long happens first, after which a short market order is sent.
5. **Long setup**:
   - EMA is below the SMA.
   - Close price is below the EMA (mirroring the "Bid < EMA" rule).
   - Existing shorts are liquidated before opening the long.
6. Every entry records the expected entry price and volume. When the position reaches zero, the strategy compares the exit price with the stored entry price. Losing trades increase the daily loss counter, enforcing the `MaxLosingTradesPerDay` filter exactly like the MQL helper `Loss()`.

## Money and Risk Management
- The strategy can work in two modes:
  - **Risk-based mode** (`UseRiskMoneyManagement = true`): position volume is sized from the account value, the `RiskPercent`, and the configured `StopLossPoints`. The calculation uses `Security.PriceStep`, `Security.StepPrice`, and `Security.VolumeStep` to mirror the MetaTrader lot sizing routine.
  - **Fixed volume mode** (`UseRiskMoneyManagement = false`): trades use the `FixedVolume` parameter.
- After the net position changes from flat to non-zero, `SetStopLoss` and `SetTakeProfit` register protective orders using `StopLossPoints` and `TakeProfitPoints` (expressed in price steps), matching the SL/TP distances passed to `m_trade.Sell/Buy` in the original expert.
- `MaxLosingTradesPerDay` stops new signals for the rest of the day once the specified number of losing trades has been detected.

## Time Management
- Trading is allowed only between `TradeStartTime` and `TradeEndTime`. The helper handles intervals that span midnight as well as daytime sessions.
- When `EnableTimeClose` is true, all open positions are liquidated after `TimeClose`, replicating the MetaTrader "TimeCloseTrue" and `TimeClose` inputs.

## Differences vs. MetaTrader Version
- The original indicator created graphical rectangles on the chart. StockSharp does not create drawing objects; instead, the same range is calculated internally via Highest/Lowest indicators.
- Losing trades are counted using closing prices from the signal candle. This matches the intention of `Loss()` (counting losing orders per day) while staying within high-level StockSharp abstractions.
- Order filling characteristics such as `ORDER_FILLING_FOK/IOC` are handled by StockSharp's environment, so explicit filling-mode configuration is not required.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `EmaPeriod` | 45 | Period of the fast EMA. |
| `SmaPeriod` | 200 | Period of the slow SMA. |
| `RangeCandles` | 10 | Number of candles forming the rectangle. |
| `RectangleSizePercent` | 0.5 | Maximum rectangle height allowed for trading. |
| `StopLossPoints` | 250 | Stop-loss distance in price steps. |
| `TakeProfitPoints` | 750 | Take-profit distance in price steps. |
| `UseRiskMoneyManagement` | true | Toggle between risk-based and fixed volume. |
| `RiskPercent` | 1 | Percentage of account equity risked per trade. |
| `FixedVolume` | 1 | Fixed volume when risk-based sizing is disabled. |
| `MaxLosingTradesPerDay` | 1 | Daily cap on losing trades. |
| `TradeStartTime` | 03:00 | Time of day when entries are allowed. |
| `TradeEndTime` | 22:50 | Time of day after which no new entries are generated. |
| `EnableTimeClose` | false | Enables end-of-day liquidation. |
| `TimeClose` | 23:00 | Time of day to close all positions. |
| `CandleType` | 1-hour candles | Primary candle data source. |

## Charting
If a chart area is available, the strategy draws the price candles, fast EMA, slow SMA, and own trades to visualize range breakouts and trade timing.
