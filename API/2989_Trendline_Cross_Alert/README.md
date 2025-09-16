# Trendline Cross Alert Strategy

## Overview
This strategy reproduces the behaviour of the original MetaTrader expert advisor that watched for price crossings of manually drawn horizontal lines and trendlines. It continuously monitors finished candles, checks whether the candle body straddled any registered level, and generates alerts the first time a crossing occurs. No automatic orders are submitted by default; the module focuses on tracking discretionary levels and informing the operator.

## Conversion highlights
- Only lines tagged with the *Monitoring Color* value are considered, mirroring the original EA that filtered objects by colour.
- Once a crossing is detected the line is flagged internally so subsequent candles do not fire duplicate alerts. This mirrors recolouring the object to the `CrossedColor` input in MetaTrader.
- Because StockSharp does not expose chart objects from the terminal, levels are defined through text parameters. Horizontal entries are parsed from `Name|Color|Price` blocks, while trendlines use `Name|Color|StartTime|StartPrice|EndTime|EndPrice` and are evaluated as infinite lines between the two anchor points.
- Alert, push notification, and email options map to informational log entries so the workflow remains transparent even without platform specific notification channels.

## Parameters
| Parameter | Type | Description |
| --- | --- | --- |
| `MonitoringColor` | `string` | Colour label that lines must match to be monitored. Case-insensitive. |
| `CrossedColor` | `string` | Label used in alert messages to indicate that the line was crossed. |
| `HorizontalLevelsInput` | `string` | Semicolon-separated list of horizontal levels. Each entry is `Name|Color|Price`; if colour is omitted the monitoring colour is assumed. |
| `TrendlineDefinitions` | `string` | Semicolon-separated list of trendlines. Each entry is `Name|Color|StartTime|StartPrice|EndTime|EndPrice`. Times must be in ISO 8601 format and use the trading calendar’s timezone. |
| `EnableAlerts` | `bool` | When `true` the strategy writes an info log entry describing the crossing. |
| `EnableNotifications` | `bool` | Adds a second log entry that emulates a push notification. |
| `EnableEmails` | `bool` | Adds a third log entry that emulates an email alert. |
| `CandleType` | `DataType` | Candle series used to monitor the market. |

## Definition format
1. Separate multiple entries with a semicolon (`;`).
2. Horizontal levels may omit the name or colour:
   - `1.1050` → monitored as `Horizontal 1` at price `1.1050` using the monitoring colour.
   - `Resistance|1.1180` → custom name while still using the monitoring colour.
   - `Breakout|Blue|1.1225` → custom colour must still match `MonitoringColor` in order to be tracked.
3. Trendlines require two anchor points with ISO 8601 timestamps (`2024-03-15T10:00:00Z`). Missing colour values default to the monitoring colour. Lines are extrapolated beyond the anchors exactly like MetaTrader’s trendlines.

## Execution flow
1. During `OnStarted` the text definitions are parsed into strongly typed structures and stored in memory.
2. Finished candles from the configured subscription trigger `ProcessCandle`.
3. The method checks whether the candle opened on one side of a level and closed on the other side. If so, the line is marked as crossed and a message is generated.
4. Messages include the crossing direction, the theoretical line price, and the close price so that discretionary traders can react manually.

## Notifications
StockSharp strategies emit log messages instead of UI pop-ups. Every enabled notification channel produces a separate log entry, allowing the hosting application to route them to actual alerting systems if needed.

## Usage checklist
1. Select the instrument and timeframe, then set the `CandleType` accordingly.
2. Fill `HorizontalLevelsInput` and `TrendlineDefinitions` with the lines drawn in your MetaTrader workspace (or any custom values).
3. Adjust the notification booleans to match the desired alert channels.
4. Start the strategy. The charting subsystem may be used to plot lines manually if desired; this module focuses on detection.

## Example configuration
```
MonitoringColor = "Yellow"
CrossedColor = "Green"
HorizontalLevelsInput = "DailyPivot|Yellow|1.1025;WeeklyHigh|Yellow|1.1100"
TrendlineDefinitions = "UpperChannel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080"
EnableAlerts = true
EnableNotifications = true
EnableEmails = false
CandleType = 15 minute candles
```
This setup watches two static levels and one ascending trendline. A message such as `Price crossed horizontal line 'DailyPivot' upward at 1.10250 ...` will be written the first time a close passes through each level.

## Risk management and extensions
- The strategy does not modify positions. Combine it with separate execution logic if automatic trading is required.
- To reset alerts, stop and restart the strategy or adjust the definition strings. Persisting the `HashSet` state is intentionally avoided to stay close to the original EA behaviour.
- Additional safeguards such as session filters or volatility checks can be layered on top by extending the `ProcessCandle` method.

