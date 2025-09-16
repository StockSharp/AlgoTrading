# Exp Breakout Signals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The EXP Breakout Signals indicator listens for price reactions around manually defined horizontal levels.
This StockSharp port reproduces the alert logic using the high-level strategy API: it subscribes to candles,
tracks level crossings, and emits descriptive log events without opening orders.

## Concept

- The trader provides a list of horizontal breakout levels (support and resistance) through the `Levels` parameter.
- Only fully formed candles are processed to avoid duplicate notifications while a bar is building.
- When a candle touches or opens across any configured level, the strategy raises the selected notification type.
- Alerts are prefixed with a custom tag so different workspaces can reuse the component without name clashes.

## Market Data Processing

1. During startup the strategy validates the notification configuration (sound mode requires a file name).
2. The level list is parsed once and cached; invalid numbers are skipped with warnings in the log.
3. A candle subscription for the configured timeframe is created via `SubscribeCandles`.
4. The last candle open is stored so subsequent bars can detect crossings between bar opens, matching the original MQL logic.

## Breakout Detection

- **Completed bars only**: `ProcessCandle` exits early unless `candle.State == CandleStates.Finished`.
- **Range penetration**: If the candle's high/low envelope contains the level, a breakout is assumed.
- **Gap open above**: If the previous open was below the level and the new open is at/above it, an upside breakout is reported.
- **Gap open below**: If the previous open was above the level and the new open is at/below it, a downside breakout is reported.
- Every satisfied condition triggers one notification per level and candle, matching the MT5 implementation.

## Notification Handling

- `Sound`: writes an info log mentioning the sound file that should be played by the hosting environment.
- `Alert`: writes a warning log to highlight the event in visual log viewers.
- `Push`: writes an info log indicating that an external push notification should be sent.
- `Mail`: writes an info log suggesting an email notification.
- Regardless of the mode, the message contains the prefix, the crossed level (in invariant culture), and the candle open time.

## Parameters

- **Candle Type** (`General`): timeframe used to build candles for breakout checks.
- **Prefix** (`Notifications`): tag prepended to every alert, mirroring the object prefix from MetaTrader.
- **Signal Mode** (`Notifications`): selects the log channel that represents the alert medium.
- **Sound Name** (`Notifications`): sound file reference required when `Signal Mode` is set to `Sound`.
- **Levels** (`Levels`): semicolon/comma/space separated price list (e.g. `1835.5;1840;1845.75`).
- **Clear On Stop** (`Levels`): if enabled the parsed level cache is reset when the strategy stops, forcing a re-parse on the next run.

## Usage Guide

1. Add the strategy to a portfolio and choose the instrument you plan to monitor.
2. Set `Candle Type` to the timeframe that matches the horizontal levels you draw in your analysis.
3. Enter the levels with decimal points using invariant formatting (`.`). Separate entries with `;`, `,` or spaces.
4. Adjust the prefix so alerts from multiple charts remain distinguishable in the unified log.
5. Pick the notification mode that fits your workflow; specify a sound file if the sound mode is selected.
6. Start the strategy. The log will record alerts every time price interacts with the configured levels on a new candle.
7. Stop the strategy when finished; enable `Clear On Stop` if you frequently change the level list between runs.

## Additional Notes

- The strategy does not send orders or manage positions. It is purely an alerting tool like the original MT5 script.
- Candle data is delivered via the high-level API, so no manual history loading is required.
- The cached previous open is reset on every stop/start cycle to prevent stale comparisons.
- Logging statements can be routed to external notifiers by the hosting StockSharp application if desired.
