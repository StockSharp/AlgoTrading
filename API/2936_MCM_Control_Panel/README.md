# MCM Control Panel Monitor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview

The **MCM Control Panel Monitor Strategy** ports the original MetaTrader sample into StockSharp. Instead of drawing a visual control panel, the strategy watches multiple timeframes and tick streams for the assigned security and writes detailed log messages whenever a new event arrives. It is meant for multi-timeframe diagnostics, feed validation, and as a starting point for more complex control-panel style tools.

The strategy does not send any orders. It only listens to market data and mirrors the behaviour of the MQL control panel by translating every event into a descriptive log entry.

## How It Works

1. When the strategy starts it subscribes to the primary candle series and, if enabled, to secondary and tertiary candle series. Each series can be set to any timeframe supported by your data source.
2. Each finished candle triggers a log entry describing the symbol, timeframe label (M1, H4, D1, etc.), close price, volume, and the time stamp of the event. The formatting mirrors the messages produced by the original MQL control panel.
3. If *Log Unfinished Candles* is enabled, intermediate updates are logged as soon as a new candle begins, which is useful for real-time monitoring while a bar is still forming.
4. When *Track Ticks* is enabled the strategy also subscribes to trade ticks and logs their price, volume, and time, effectively replacing the "new tick" event from the MetaTrader version.
5. All subscriptions are created through the high-level StockSharp API (`SubscribeCandles` / `SubscribeTicks`) so the strategy can be plugged directly into Designer, Shell, or API-based hosts.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Primary Timeframe** | Mandatory candle timeframe that is always logged. | 5-minute candles |
| **Use Secondary Timeframe** | Enables a second candle subscription. | Disabled |
| **Secondary Timeframe** | Timeframe for the optional secondary stream. | 15-minute candles |
| **Use Tertiary Timeframe** | Enables a third candle subscription. | Disabled |
| **Tertiary Timeframe** | Timeframe for the optional tertiary stream. | 1-hour candles |
| **Track Ticks** | Adds a tick subscription and logs every trade. | Enabled |
| **Log Unfinished Candles** | Logs candle updates before they close. Useful to trace new-bar events. | Disabled |

## Usage Notes

- Assign the desired security before starting the strategy. All log entries reference the `Security.Id` so you can easily filter them in the StockSharp log viewer.
- The strategy is read-only. It does not register orders or manage positions, making it safe to run alongside other trading bots for monitoring purposes.
- Combine the strategy with several log panels in Designer to observe how different timeframes align in real time.
- To emulate the original control panel workflow you can run multiple copies of the strategy, each attached to a different instrument, or use the secondary/tertiary timeframes to watch several horizons on the same symbol.
- Enable *Log Unfinished Candles* if you need to know exactly when a new bar starts forming on a timeframe. Leave it disabled to only log confirmed bar closes.

## Mapping to the Original MQL Script

| MQL Component | StockSharp Adaptation |
|---------------|----------------------|
| `InitControlPanelMCM` color and font configuration | Replaced by strategy parameters; StockSharp logging handles visual output. |
| `OnChartEvent` for `CHARTEVENT_CUSTOM` new-bar codes | Candle subscriptions for each timeframe; log messages display the timeframe label. |
| `CHARTEVENT_TICK` handling | Optional tick subscription with the same log formatting. |
| Printed messages (`TimeToString(...) -> id=...`) | `AddInfoLog` entries showing the symbol, timeframe name, price, volume, and time stamp. |

## Example Log Output

```
[EURUSD] M5 closed candle price=1.09845 volume=27 time=2024-03-05T09:35:00.0000000Z
[EURUSD] Tick price=1.09852 volume=1 time=2024-03-05T09:35:07.2510000Z
```

These messages confirm that the strategy has received a finished five-minute candle and a subsequent tick, mirroring the information reported by the MQL control panel.
