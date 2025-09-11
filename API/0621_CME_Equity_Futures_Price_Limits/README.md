# CME Equity Futures Price Limits

This strategy calculates daily price limit levels for CME equity futures. It captures a reference price at a specified hour and computes limit up/down (+/-5%) as well as -7%, -13%, and -20% limit-down levels. Results are written to the log for monitoring.

## Parameters

- **ManualReference** – manual reference price override (0 to disable).
- **ShowLimitDownLevels** – enable logging of -7/-13/-20% levels.
- **OffsetHour** – hour (0-23) to capture the reference price.
- **CandleType** – candle type to process (default 1 minute).
