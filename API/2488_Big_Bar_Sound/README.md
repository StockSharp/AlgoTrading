# Big Bar Sound Strategy

## Overview
The **Big Bar Sound Strategy** reproduces the behaviour of the MetaTrader expert advisor "BigBarSound". The algorithm watches finished candles of a configurable timeframe and reports whenever the candle range is wide enough to be considered a "big bar". Instead of playing an audio file it writes detailed log messages, which can be further routed to any notification subsystem supported by StockSharp.

The strategy is purely informational – it does not submit orders or manage positions. It is designed to be used as an alerting component inside a larger automated or discretionary trading workflow.

## Behaviour
1. The strategy subscribes to the candle series specified by the **Candle Type** parameter.
2. For each completed candle it measures the bar size according to the selected **Difference Mode**:
   - **OpenClose** – absolute difference between close and open price.
   - **HighLow** – absolute difference between the high and the low of the bar.
3. The measured value is compared against the **Point Threshold** multiplied by the instrument's `PriceStep`. When the bar size is greater than or equal to this threshold, the strategy records a log entry that simulates playing the configured sound file.
4. If **Show Alert** is enabled, an additional alert-style log message is written to highlight the event.

Because the implementation processes only finished candles, each bar can trigger at most once, mirroring the single-shot behaviour of the original MQL expert advisor.

## Parameters
- **Point Threshold (`BarPoint`)** – number of price steps that must be exceeded before an alert is triggered. The default value of 200 matches the original script. Optimisation boundaries (50–500 with step 50) are provided for convenience.
- **Difference Mode (`DifferenceMode`)** – selects how the candle size is measured: open/close distance or full high/low range.
- **Sound File (`SoundFile`)** – name of the WAV file that should be played. The strategy only logs this value to emulate the MetaTrader `PlaySound` call.
- **Show Alert (`ShowAlert`)** – when enabled the strategy emits an extra log message to mimic the optional `Alert` popup from the MQL version.
- **Candle Type (`CandleType`)** – candle data type (timeframe) to subscribe to. By default the strategy uses 1-minute candles.

## Alerts and logging
The strategy uses `LogInfo` to announce that the sound file would have been played and `AddInfoLog` to provide a separate alert message. These entries contain the instrument identifier, the candle timestamp and the measured bar size, making it easy to integrate with StockSharp's logging viewers or notification sinks.

If the broker does not supply a valid `PriceStep`, a fallback value of `1` is used so that the strategy remains operational. Adjust the **Point Threshold** accordingly to reflect the actual tick size of the instrument.

## Usage notes
- Attach the strategy to any instrument that exposes candle data. The alert works equally well on forex, futures, stocks or crypto assets.
- Combine it with other trading strategies by subscribing to its log output or by extending the class to forward events to custom handlers.
- Since the implementation does not generate orders, `Volume` and position-related parameters are ignored.
- To produce audible notifications, connect StockSharp's logging subsystem to a sound notifier or extend the code to call platform-specific audio APIs.

## Differences from the original MQL expert advisor
- The original script operated on tick data and tracked bar changes manually. The StockSharp port processes finished candles directly, which guarantees exactly one alert per bar without maintaining a separate trigger flag.
- Audio playback is replaced with log messages so that the behaviour remains cross-platform within the StockSharp environment.
- Parameter names follow StockSharp conventions but retain the same semantics: threshold size in points, measurement mode, optional alert and sound name.

## Requirements
No additional indicators are required. Simply ensure that the selected `CandleType` is supported by the connected data source so that the strategy receives completed candles for processing.
