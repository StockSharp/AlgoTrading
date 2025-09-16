# Peak Volume Counter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Peak Volume Counter Strategy** is a direct StockSharp port of the MetaTrader 4 utility from `MQL/9514/AIS7PVC.MQ4`. It does not send any orders. Instead, it continuously monitors tick-by-tick activity and reports bursts of cumulative trade volume that may precede liquidity spikes or news-driven moves. The logic mirrors the original "Peak Volume Counter" indicator and keeps the colour-coded volume bands as textual severity labels.

## How It Works

1. When the strategy starts it resets its internal counters, the last trade price snapshot and the timing window.
2. It subscribes to the instrument's tick stream (`DataType.Ticks`). Every trade message increases the aggregated volume. If an exchange does not provide trade volume for a tick, the strategy treats it as a single lot to match the behaviour of MetaTrader's tick-volume counter.
3. The first trade defines the start of the accumulation window. Subsequent trades keep extending the window until the configured threshold is reached.
4. Once the aggregated volume equals or exceeds the `VolumeThreshold` parameter (default `7`), the strategy writes a log entry describing the event, the elapsed time since the window started, the last trade price, the server and local timestamps, and the severity label derived from the original colour scale.
5. After logging the alert, the counters reset and the strategy immediately starts accumulating a new window, allowing consecutive peaks to be reported without gaps across minute boundaries.

The approach reproduces the original MQL logic where the script compared the tick volume of consecutive one-minute candles and flagged strong bursts. By operating directly on tick data the StockSharp version captures the same behaviour while avoiding the limitations of candle updates.

## Parameters

| Name | Description | Default | Optimisable |
| --- | --- | --- | --- |
| `VolumeThreshold` | Minimum cumulative trade volume required to trigger an alert. The aggregation resets after each alert. | `7` | Yes (5 → 20, step 1) |

## Alert Details

Alerts are written through the strategy log (`LogInfo`). A sample message looks like:

```
Peak volume #3: volume 11 (yellow), window 4.5s, price 1.2345, server 2024-05-01T12:34:56.7890000Z, local 2024-05-01T15:34:56.7900000+03:00.
```

The severity label follows the original colour bands:

| Severity label | Original colour range | Description |
| --- | --- | --- |
| `red` | 7 – 8 ticks | Initial warning that minimal burst size was reached. |
| `orange` | 9 – 10 ticks | Moderate acceleration in trade flow. |
| `yellow` | 11 – 12 ticks | Elevated activity signalling stronger participation. |
| `lawn green` | 13 – 14 ticks | High activity that may precede a breakout. |
| `aqua` | 15 – 16 ticks | Very strong flow requiring attention. |
| `blue` | 17 – 18 ticks | Rarely observed surges that often align with major events. |
| `violet` | 19+ ticks | Extreme bursts indicating an exceptional liquidity shock. |

The reported window duration is the time between the first and the last trade that contributed to the burst. If the trading venue sends out-of-order timestamps, the duration is clamped to zero to avoid negative values.

## Usage Notes

- Attach the strategy to a connection that supplies real tick data. Without trade messages no alerts are generated.
- Because no orders are placed, the module is safe to run on live connections as a monitoring assistant.
- Optimise `VolumeThreshold` to match the liquidity profile of the target symbol. Illiquid instruments may need a lower threshold, while index futures and FX pairs can work with higher values.
- The port keeps the original indicator's behaviour across minute transitions by accumulating volume continuously. There is no reliance on candle completion, so alerts are not delayed until the end of a bar.
- Combine the alerts with your own trading logic or visual dashboard by subscribing to the strategy log and reacting to the severity labels.

