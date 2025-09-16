# ZigZag EA

## Overview
The strategy replicates the original MT5 "ZigZag EA" logic by waiting for three consecutive ZigZag swing points and placing two breakout stop orders around the previous swing range. The conversion uses the StockSharp high level API and works with finished candles. The last two completed swings define a trading corridor, while the most recent swing ("room 0" in the MQL version) must remain inside that corridor before the strategy arms itself with pending orders. The approach is symmetrical: it prepares both buy-stop and sell-stop orders and lets the market decide the direction of the breakout.

## Indicators and market data
* **Highest / Lowest:** StockSharp does not expose the MT ZigZag indicator directly, therefore the conversion mimics ZigZag behaviour by tracking the rolling highest and lowest values over the selected depth. Direction changes update the internal swing buffers exactly like the original EA reading the ZigZag buffer.
* **Candles:** the strategy subscribes to a configurable candle type (default: 1 minute time frame) and works only with finished candles to stay compatible with backtesting and real trading.

## Trade logic
1. Collect the latest three swing values. The two previous values determine the corridor (`high`/`low`), and the last value must remain inside the corridor with a small buffer defined by the broker stop level.
2. Enforce corridor size limits (`MinCorridorPips` and `MaxCorridorPips`). Too narrow corridors are ignored to avoid noise, while overly wide corridors are filtered out to avoid enormous stops.
3. Once the corridor is valid and no position is open, place symmetrical pending orders:
   * **Buy stop** at `high + EntryOffsetPips`.
   * **Sell stop** at `low - EntryOffsetPips`.
4. Stops and targets are computed from Fibonacci ratios exactly as in the MQL implementation: `FiboStopLoss` multiplies the corridor height and `FiboTakeProfit` subtracts the corridor from the selected Fibonacci projection. Prices are rounded to the instrument tick size to avoid rejections.
5. When a pending order triggers, the remaining pending order is cancelled and the protective stop-loss and take-profit are registered immediately. Optional trailing logic tightens the stop when price travels `TrailingStepPips` beyond the trailing distance.
6. The strategy closes and re-arms itself automatically when the position returns to zero.

## Risk and order management
* Protective stop and target orders are live stop/limit orders, so the broker controls execution and gaps are handled naturally.
* The trailing stop logic is lifted from the EA: it activates after the profit exceeds `TrailingStopPips + TrailingStepPips` and then re-registers the stop every time the distance increases by at least one trailing step.
* The strategy uses the base `Volume` parameter of the StockSharp `Strategy` class. Money management blocks from the MQL version (fixed lot vs. risk percentage) are intentionally omitted because position sizing is usually broker specific in StockSharp.

## Session filter
* Trading is allowed only between `StartHour:StartMinute` and `StopHour:StopMinute`. If the stop time is earlier than the start time the strategy treats it as an overnight session and allows trading across midnight.
* Pending orders are cancelled whenever the session is closed, mirroring the MQL behaviour that removed orders outside the allowed window.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Candle series used for analysis. | 1 minute candles |
| `ZigZagDepth` | Number of candles for swing detection. | 12 |
| `EntryOffsetPips` | Offset added above/below the corridor. | 5 |
| `MinCorridorPips` | Minimum corridor height to validate a setup. | 20 |
| `MaxCorridorPips` | Maximum corridor height allowed. | 100 |
| `FiboStopLoss` | Fibonacci level used to compute stop-loss distance. | 61.8% |
| `FiboTakeProfit` | Fibonacci level used for profit target. | 161.8% |
| `StartHour` / `StartMinute` | Beginning of the trading window. | 00:01 |
| `StopHour` / `StopMinute` | End of the trading window. | 23:59 |
| `TrailingStopPips` | Distance used by the trailing stop. | 5 |
| `TrailingStepPips` | Minimum improvement required to move the trailing stop. | 5 |
| `DrawCorridorLevels` | If enabled the strategy draws a vertical corridor marker on the chart for reference. | `false` |

## Implementation notes
* Pip values are calculated from the instrument tick size. Instruments with 3 or 5 decimal places automatically multiply the tick by 10, replicating the "adjusted point" logic used in the EA.
* The code uses high level helper methods such as `BuyStop`, `SellStop`, `SellLimit`, and `BuyLimit`, in line with the project guidelines.
* Comments are kept in English to match the repository requirements, while the detailed description is provided in three languages across the README files.
* No Python port is created; the folder contains only the C# implementation as requested.
