# Spread Informer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Collects detailed statistics for the bid-ask spread of the selected instrument and notifies when the spread breaks a configurable limit. The strategy continuously listens to Level1 updates, tracks maximum, minimum, and average spread in points, and logs a summary once it stops. It is useful for researching liquidity conditions before running latency-sensitive systems or optimizing trading windows in the Strategy Tester.

## Details

- **Data Source**: Level1 best bid and best ask quotes.
- **Statistics Captured**:
  - Start and end timestamps of the observation period.
  - Maximum spread and the time when it occurred.
  - Minimum spread and the time when it occurred.
  - Average spread calculated across all observed Level1 samples.
- **Alerts**:
  - Optional alert when the spread (in points) rises above the configured `MaxSpreadPoints` threshold.
  - Alert frequency is limited by `AlertIntervalSeconds` to avoid spamming the log.
  - Alerts are only triggered when the spread crosses the threshold from below.
- **Logging**:
  - Real-time alerts are written through `LogInfo`.
  - Final statistics summary is emitted during `OnStopped`.
- **Default Values**:
  - `MaxSpreadPoints` = 0 (alerts disabled).
  - `AlertIntervalSeconds` = 0 (no throttling).

## Parameters

| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `MaxSpreadPoints` | Maximum allowed spread in points. Set to 0 to disable alerts. | 0 | Points are calculated using the instrument price step. |
| `AlertIntervalSeconds` | Minimum time between consecutive alerts. | 0 | Prevents duplicate alerts when the spread stays wide. |

## Usage Notes

1. Attach the strategy to an instrument and ensure Level1 data is available.
2. Configure `MaxSpreadPoints` according to the acceptable spread for the instrument.
3. Optionally increase `AlertIntervalSeconds` to suppress repeated notifications during volatile periods.
4. Stop the strategy to review the logged statistics in the terminal output.
