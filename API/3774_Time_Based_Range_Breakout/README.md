# Time-Based Range Breakout Strategy

## Overview
This strategy is a direct port of the MetaTrader 4 expert advisor `Tttttt_www_forex-instruments_info.mq4`. It builds intraday breakout levels once per day at a configurable time. Whenever price closes beyond those levels, the strategy opens a position in the breakout direction. Exits are managed by dynamic profit and loss distances that are derived from an average of historical day ranges.

## Core Logic
1. **Daily snapshot time** – At `CheckHour:CheckMinute` the strategy freezes the current day's high and low and closes any open positions.
2. **Average range calculation** – The algorithm aggregates the last `DaysToCheck` statistics:
   - *CheckMode = 1*: uses the full high/low range of each completed day.
   - *CheckMode = 2*: uses the absolute difference between the check-time closes of consecutive days.
3. **Level construction** – The average value is divided by `OffsetFactor` to create an upper and lower breakout band around the current day's high/low. The same average is divided by `ProfitFactor` and `LossFactor` to derive dynamic take-profit and stop distances.
4. **Entry window** – After the daily snapshot the strategy watches the candle closes until 23:00. If a close price pierces the upper band and no position is open, it buys; if the lower band is broken, it sells. The number of entries per day is limited by `TradesPerDay`.
5. **Exit management** – While in a position, the strategy compares the close price with the average entry price (`Strategy.PositionPrice`). Once the move in favor or against reaches the configured profit or loss distances, the position is closed at market. If `CloseMode = 2`, any leftover position is also closed at the start of the next trading day.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CheckHour` | Hour (0-23) when the daily range snapshot is taken. | `8` |
| `CheckMinute` | Minute (0-59) when the snapshot is taken. | `0` |
| `DaysToCheck` | Number of historical days used for averaging. | `7` |
| `CheckMode` | `1` = use daily high/low range, `2` = use absolute difference between consecutive check-time closes. | `1` |
| `ProfitFactor` | Divides the averaged value to obtain the profit target distance. | `2` |
| `LossFactor` | Divides the averaged value to obtain the loss distance. | `2` |
| `OffsetFactor` | Divides the averaged value to obtain the breakout offset around high/low. | `2` |
| `CloseMode` | `1` = keep positions overnight, `2` = flatten when the calendar day changes. | `1` |
| `TradesPerDay` | Maximum number of entries allowed per day. | `1` |
| `CandleType` | Candle series used for all calculations (defaults to 15-minute candles). | `15m` time frame |

All parameters are created through `Strategy.Param` so they support optimization out of the box.

## Differences from the MQL Version
- MetaTrader tracks floating profit directly; the StockSharp port reconstructs it from `Position` and `PositionPrice` when evaluating exits.
- MT4 code counted active orders via ticket loops. The port uses `TradesPerDay` together with the aggregate position to keep the number of same-day trades under control.
- The original script relied on historical buffers (e.g., `Highest`, `Lowest`). The StockSharp version stores daily statistics internally, avoiding explicit indicator buffers while respecting the high-level API guidelines.
- Protective stop-loss and take-profit orders were sent together with the market entry in MT4. The port performs equivalent risk control by monitoring candle closes and sending market exit orders when thresholds are reached.

## Usage Notes
- Use a candle series that matches the bar size of the original MQL setup (15-minute bars were used in the reference file).
- Provide at least `DaysToCheck` completed days of historical data before starting the strategy, otherwise breakout levels will remain inactive.
- When optimizing, keep the factors positive to maintain meaningful breakout and risk thresholds.
