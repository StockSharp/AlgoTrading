# HPCS Fifth MT4 EA V01 WE Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **HPCS Fifth MT4 EA V01 WE Strategy** is a faithful StockSharp port of the MetaTrader 4 expert `_HPCS_Fifth_MT4_EA_V01_WE.mq4`. The original expert is purely informational: it monitors the active chart and issues an alert whenever a brand-new candle appears. This C# implementation mirrors that behavior by subscribing to the selected candle series, filtering unfinished updates, and logging a user-defined notification the moment a bar is completed. Optional sound logging replicates the `PlaySound` call from MetaTrader, making the strategy useful as a lightweight session monitor or a building block for more complex automation scripts.

Unlike trading algorithms, this strategy never submits orders. Its purpose is to keep operators informed about chart activity across any timeframe supported by StockSharp. The provided parameters allow operators to choose the candle type, personalize the alert text, and document the sound file that should be played by external infrastructure.

## Notification Logic
1. Subscribe to the candle series defined by `CandleType` when the strategy starts.
2. Process only candles whose state is `Finished`; unfinished updates are ignored to match MetaTrader's closed-bar logic.
3. Track the last processed open time. If the incoming candle shares the same open timestamp, skip it to avoid duplicate alerts.
4. Once a unique finished candle is detected, write an informational log entry that contains the configured `AlertMessage` and the candle open time in ISO 8601 format.
5. When `PlaySound` is enabled and `SoundFile` is not empty, add a second log entry indicating which sound resource should be triggered.

This flow replicates the MT4 expert's combination of `Alert()` and `PlaySound()` in a platform-neutral way that integrates with StockSharp logging, notifications, and visualization.

## Parameters
- `CandleType` – data type specifying the candle timeframe to monitor (default: 1-minute time-frame candles).
- `AlertMessage` – text emitted in the informational log when a new candle is confirmed (default: `"New Candle Generated"`).
- `PlaySound` – boolean flag toggling the additional log entry that references the configured sound file (default: `true`).
- `SoundFile` – name of the sound resource associated with the alert (default: `"alert.wav"`).

Set the strategy `Volume` property only if the template is reused inside a composite algorithm; this strategy itself does not place trades.

## Usage Tips
- Combine the strategy with StockSharp desktop notifications or external log listeners to trigger pop-up reminders, email dispatchers, or custom sound players on new bars.
- Deploy multiple instances with different `CandleType` values to supervise distinct timeframes (for example, 1-minute for scalping and 1-hour for swing trades).
- Adjust `AlertMessage` to include human-readable context such as the instrument name, timeframe, or trading session.
- When running inside a bot framework, the emitted log entries can be used to synchronize other modules that must wait for the close of a candle before acting.

## Implementation Notes
- The high-level API (`SubscribeCandles` + `Bind`) delivers candle updates without manual series management, mirroring the MetaTrader `Time[]` array semantics.
- Finished candle filtering enforces the closed-bar requirement and prevents repeated alerts during the formation of the same candle.
- The strategy stores only the latest candle open time, avoiding in-memory collections and complying with the conversion guidelines.
- Visualization hooks (`CreateChartArea` and `DrawCandles`) are invoked when a chart surface is available, allowing operators to inspect the underlying candle flow visually.
- All comments and log messages are in English, as required by the conversion rules, ensuring clarity in multilingual environments.

Although the strategy is simple, it demonstrates how MetaTrader notification scripts can be ported to StockSharp while embracing high-level abstractions and parameter-driven configuration.
