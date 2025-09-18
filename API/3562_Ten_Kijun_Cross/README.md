# TenKijun Cross Alert Strategy (ID 3562)

## Overview

This strategy is a StockSharp high-level API port of the MetaTrader expert advisor **TenKijun.mq4**. The original EA only watches the Ichimoku indicator and sends push notifications when the Tenkan-sen (conversion line) crosses the Kijun-sen (base line). The C# version keeps the alert-only nature but upgrades the implementation with StockSharp infrastructure, chart bindings, parameterization and safe session handling.

The logic works on completed candles of a configurable timeframe. When a new candle closes within the active trading hours, the strategy evaluates the Ichimoku indicator calculated with the classic 9/26/52 periods and records the latest Tenkan/Kijun values. If the Tenkan crosses above the Kijun an informational message is logged indicating a bullish cross; if Tenkan crosses below Kijun a bearish alert is logged. No trades are executed – the strategy is intended for signal generation or to be combined with external automation.

## Indicator and Data Flow

- **Indicator** – StockSharp `Ichimoku` indicator with separately parameterized Tenkan, Kijun and Senkou Span B lengths. Only the Tenkan and Kijun lines are used for decision making, mirroring the original EA.
- **Data subscription** – Uses `SubscribeCandles` with a configurable `CandleType`. By default, 30-minute timeframe candles are requested.
- **Binding** – `BindEx` is employed so that the typed `IchimokuValue` is delivered to the handler without manual calls to `GetValue`.
- **Charting** – Candles and the Ichimoku indicator are attached to the strategy chart automatically for quick visual validation of alerts.

## Trading Session Filter

The MetaTrader script restricted alerts to a user-defined session window. The port exposes the same feature via two parameters:

- `StartHour` – inclusive start of the active window (default 0). Accepts 0-23.
- `LastHour` – inclusive end of the active window (default 20). Accepts 0-23.

If `StartHour` is less than or equal to `LastHour`, alerts are produced between those two hours of the day. If the start is greater than the end, the window is treated as overnight (for example 20 → 6 covers the late-evening to early-morning session).

## Parameters

| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `StartHour` | Hour when alerts can begin. | 0 | Inclusive, 0-23 range. |
| `LastHour` | Hour when alerts stop. | 20 | Inclusive, 0-23 range. |
| `TenkanPeriod` | Conversion line lookback. | 9 | Optimizable. |
| `KijunPeriod` | Base line lookback. | 26 | Optimizable. |
| `SenkouSpanBPeriod` | Leading span B lookback. | 52 | Provided for completeness even though alerts do not depend on the cloud. |
| `CandleType` | Candle series used for the indicator. | 30-minute timeframe | Choose any `TimeSpan`-based timeframe. |

## Alert Logic

1. Wait for the first finished candle to initialize Tenkan and Kijun history.
2. On each subsequent finished candle within the trading window:
   - Extract Tenkan and Kijun values from the Ichimoku indicator.
   - Detect a bullish cross when the previous Tenkan was less than or equal to the previous Kijun and the current Tenkan is greater than the current Kijun.
   - Detect a bearish cross when the previous Tenkan was greater than or equal to the previous Kijun and the current Tenkan is less than the current Kijun.
   - Emit an informational log entry describing the direction, price and timestamp of the cross.

## Usage Tips

- Combine this strategy with StockSharp notification adapters (email, Telegram, sound) by subscribing to the strategy log or by extending the `ProcessCandle` method with custom notification code.
- To drive automated trading, inherit from `TenKijunCrossStrategy` and override `ProcessCandle` to place orders instead of – or in addition to – logging messages.
- Adjust the candle timeframe to match the original MetaTrader chart used by the EA to keep alerts aligned.

## Differences from the Original EA

- Uses StockSharp logging instead of MetaTrader `SendNotification`. The behaviour remains alert-only but relies on the platform’s message pipeline.
- Adds full parameter metadata (`SetDisplay`, ranges, optimization flags) making the strategy ready for Designer/Optimizer tools.
- Automatically draws candles and the Ichimoku indicator in the StockSharp chart window when available.

## Files

- `CS/TenKijunCrossStrategy.cs` – main C# implementation of the alert logic.

