# Record Spread Stop Freeze Level Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Record Spread Stop Freeze Level Strategy is a utility strategy that periodically records market microstructure values for one or more securities. It mirrors the behavior of the original MetaTrader expert by collecting spread, stop level and freeze level metrics and saving them into a delimited log file for later analysis.

## Details

- **Purpose**: monitoring tool that produces time series of spread, stop and freeze levels
- **Data Sources**:
  - Level1 quotes for each monitored security (best bid/ask and provider specific metadata)
  - Connector state to detect current server time and connection status
- **Logging Interval**: configurable timer in minutes (default 1 minute)
- **Securities**:
  - Optionally include the primary strategy security
  - Accepts an explicit list of additional `Security` instances to monitor in parallel
- **Output**:
  - CSV-like text file stored in the platform `Logs` folder
  - Automatic archival of the previous log into the `Logs/BUP` subfolder before a new session starts

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `RecordPeriodMinutes` | Interval (in minutes) between timer callbacks that trigger a log entry. Must be greater than zero. | `1` |
| `LogFilePrefix` | Prefix that is prepended to the generated log file name. | `"MktData"` |
| `IncludePrimarySecurity` | When `true`, the strategy security is added to the monitoring list. | `true` |
| `AdditionalSecurities` | Extra securities that should be tracked together with the primary one. | empty |

## Log File Structure

- **Path**: `<application base>/Logs/<prefix>_Acc_<account>.csv`
  - `<application base>` is `AppDomain.CurrentDomain.BaseDirectory`
  - `<account>` is taken from the bound portfolio name; if unavailable the strategy identifier is used
- **Backup**: before writing, the previous file is copied into `Logs/BUP/<prefix>_Acc_<account>.csv`
- **Header**:
  - `TimeLocal;TimeServer;IsConnected;SYMBOL_Spread;SYMBOL_StopLevel;SYMBOL_FreezeLevel;...`
  - `SYMBOL` is derived from `Security.Id` and sanitized for file safety
- **Rows**:
  - `TimeLocal`: local workstation time in ISO-8601 format (`DateTimeOffset.Now`)
  - `TimeServer`: connector time (`Strategy.CurrentTime`) in ISO-8601 format
  - `IsConnected`: `True` when the connector reports a live connection, otherwise `False`
  - `Spread`: computed as `BestAsk - BestBid` when both values are available; `N/A` otherwise
  - `StopLevel` / `FreezeLevel`: populated only if the data provider supplies corresponding Level1 fields; missing values are recorded as `N/A`

## Usage Notes

1. Assign the strategy to a portfolio/connector that publishes Level1 data for every monitored security.
2. Configure the `AdditionalSecurities` parameter with the instruments that should be sampled together.
3. Optionally change the log prefix and recording interval to match reporting requirements.
4. Start the strategy: it subscribes to Level1 streams, initializes a fresh log file and appends a new row on every timer tick.
5. Inspect the produced file inside the `Logs` directory. Each run archives the previous file automatically.

## Limitations

- Stop and freeze level fields are broker specific. When the connected provider does not expose the corresponding Level1 values the file contains `N/A` placeholders.
- The timer resolution is one minute; shorter sampling would require lowering the parameter and ensuring the trading environment supports the chosen frequency.
- The utility does not place orders or manage positions—it is intended strictly for diagnostics and data collection.
