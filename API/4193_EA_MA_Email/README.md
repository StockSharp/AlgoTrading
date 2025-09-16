# EA MA Email Alert Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
- Replicates the MetaTrader 4 expert "EA_MA_Email" by monitoring a set of exponential moving averages (EMAs) calculated from candle open prices.
- Generates informational log entries that imitate the original email alerts whenever the chosen EMA pairs cross each other upward or downward.
- Designed as a non-trading utility: it never sends orders and can be attached to any instrument just to receive crossover notifications.

## Indicator Setup
- **EMA 20, EMA 50, EMA 100, EMA 200**
  - All indicators are calculated on the candle open price to match the source MQL implementation.
  - The selected candle timeframe drives the EMA updates; choose any timeframe supported by the StockSharp time-frame candles.

## Signal Logic
1. The strategy subscribes to the configured candle type and feeds the open prices into the four EMA indicators.
2. Once all EMAs are formed, the strategy tracks the previous and current values for each pair that is enabled through parameters.
3. A bullish alert is produced when the faster EMA was previously below the slower EMA and closes above it on the current candle.
4. A bearish alert is produced when the faster EMA was previously above the slower EMA and closes below it on the current candle.
5. Alerts are logged with a subject/body structure that mirrors the MT4 email, including instrument identifier, crossover direction, candle period name, timestamp, and closing price.

## Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SendEmailAlert` | `bool` | `true` | Enables writing simulated email alerts to the strategy log. |
| `Monitor20Over50` | `bool` | `false` | Track crossovers between EMA 20 and EMA 50. |
| `Monitor20Over100` | `bool` | `false` | Track crossovers between EMA 20 and EMA 100. |
| `Monitor20Over200` | `bool` | `false` | Track crossovers between EMA 20 and EMA 200. |
| `Monitor50Over100` | `bool` | `false` | Track crossovers between EMA 50 and EMA 100. |
| `Monitor50Over200` | `bool` | `true` | Track crossovers between EMA 50 and EMA 200. |
| `Monitor100Over200` | `bool` | `false` | Track crossovers between EMA 100 and EMA 200. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Candle source that defines the EMA calculations and alert timing. |

## Alert Format
- Subject example: `EURUSD 50>200 PERIOD_H1`
- Body example: `Date and Time: 2024-05-08 13:00:00; Instrument: EURUSD; Close: 1.07543`
- Every alert is emitted through `AddInfoLog`, so it can be routed to any preferred logging sink or notification hub.

## Usage Notes
- Because alerts rely on completed candles, intrabar movements do not create notifications until the bar finishes.
- The helper converts common MetaTrader periods (M1, M5, M15, M30, H1, H4, D1, W1, MN1) to human-readable strings identical to the original expert.
- Disable `SendEmailAlert` to stop logging while keeping the EMA calculations active, for instance when combining with other strategies.
- The strategy is safe to run on live connections because it never submits trading orders.
