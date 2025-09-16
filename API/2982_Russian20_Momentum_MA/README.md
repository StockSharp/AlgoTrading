# Russian20 Momentum MA Strategy

## Overview
The **Russian20 Momentum MA Strategy** is a direct conversion of the MetaTrader 5 expert advisor `Russian20-hp1.mq5`. The original script was published by Gordago Software Corp. and relies on a two-hour chart, a 20-period simple moving average (SMA), and a 5-period Momentum indicator to identify short-term trend continuations. The StockSharp implementation keeps the same analytical core while adapting order handling and money management to the high-level strategy API.

## Trading Logic
- **Data frequency:** Works with the user-defined candle type (default is 2-hour candles, matching the MQL5 timeframe `PERIOD_H2`). The logic is executed only when a candle is closed.
- **Indicators:**
  - Simple Moving Average with configurable period (default 20).
  - Momentum indicator with configurable period (default 5). The neutral Momentum level is 100, mirroring the MQL5 default output.
- **Long entry:** Triggered when all of the following conditions are satisfied on the latest closed candle:
  1. Close price is above the SMA.
  2. Momentum value is greater than 100 (positive acceleration).
  3. The close price is higher than the previous candle’s close, ensuring upward momentum in price action.
- **Short entry:** Triggered when all of the following conditions are satisfied:
  1. Close price is below the SMA.
  2. Momentum value is less than 100 (negative acceleration).
  3. The close price is lower than the previous candle’s close.
- **Long exit:** The strategy liquidates long positions when Momentum drops below 100 or when a protective stop-loss or take-profit threshold is crossed.
- **Short exit:** The strategy liquidates short positions when Momentum rises above 100 or when the configured protective thresholds are reached.

## Risk Management
The original MQL5 expert places fixed stop loss and take profit orders in “pips” that are adjusted for 4- and 5-digit Forex pricing. The C# conversion reproduces this behaviour by:
- Calculating an adjusted pip size from the security’s `PriceStep`. For symbols with three or five decimal places, the pip size equals `PriceStep * 10`, otherwise it equals `PriceStep`.
- Translating the user inputs for stop loss and take profit into absolute price distances.
- Monitoring price action on each closed candle and closing the position when the price crosses the calculated thresholds.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 2-hour candles | Data type used for signal generation. |
| `MovingAverageLength` | 20 | Lookback for the SMA filter. |
| `MomentumPeriod` | 5 | Lookback for the Momentum indicator. |
| `StopLossBuyPips` | 50 | Long stop-loss distance expressed in pips. Set to 0 to disable. |
| `TakeProfitBuyPips` | 50 | Long take-profit distance in pips. Set to 0 to disable. |
| `StopLossSellPips` | 50 | Short stop-loss distance in pips. Set to 0 to disable. |
| `TakeProfitSellPips` | 50 | Short take-profit distance in pips. Set to 0 to disable. |

All numeric parameters are exposed through `StrategyParam<T>` and marked as optimizable when applicable, enabling backtesting and optimisation with StockSharp tools.

## Implementation Notes
- The strategy uses the high-level `SubscribeCandles().Bind(...)` API to stream candle data and simultaneously obtain SMA and Momentum values without manual indicator bookkeeping.
- Momentum levels are evaluated exactly as in the MQL5 script (100 as the neutral level). Any breach beyond the stop-loss/take-profit offsets triggers a market exit, faithfully mimicking the original order placement logic.
- The previous close is cached to verify price momentum without resorting to historical collection lookups, in line with the project’s performance guidelines.
- Visualization hooks (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) are wired for convenience when the host environment supports charting.

## Usage Tips
- The default timeframe and parameters correspond to the author’s original configuration. Adjust the candle type when working with instruments that do not produce 2-hour bars.
- When trading assets quoted with unconventional tick sizes, review the calculated pip size to make sure the stop-loss and take-profit distances remain realistic.
- The strategy is designed for a single open position at a time. External manual trades or simultaneous positions on the same security may interfere with the built-in exit logic.
