# ReInitChart Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader **ReInitChart** utility to StockSharp. The original script created a button on every chart that temporarily switched the timeframe to force indicators to recompute. The StockSharp version keeps the same spirit by exposing a manual refresh toggle and optional automatic timer that reset the internal SMA indicator and log the refresh event. A simple SMA trend-following rule is applied to demonstrate trading once the indicator is rebuilt.

## How It Works

1. **Primary data feed** – the strategy subscribes to the timeframe defined by `CandleType` and calculates a simple moving average with length `SmaLength`.
2. **Manual refresh** – when `ManualRefreshRequest` becomes `true`, the moving average state is reset, the flag is cleared, and the action is reported in the log together with the preserved button metadata (`RefreshCommandName`, `RefreshCommandText`, `TextColorName`, `BackgroundColorName`).
3. **Automatic refresh** – enabling `AutoRefreshEnabled` schedules recurring resets every `AutoRefreshInterval`, reproducing the timer-driven reinitialisation from MetaTrader.
4. **Trading logic** – after the SMA is formed, the strategy maintains at most one position. It goes long when the close price is above the SMA and flips short when the price falls below it, closing the opposite side first.

This behaviour mirrors the idea of reinitialising all charts from the original Expert Advisor while using idiomatic StockSharp components (indicator reset and logging) instead of switching chart timeframes.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Working timeframe for candle subscription. |
| `SmaLength` | Number of candles used for the moving average that is rebuilt after each refresh. |
| `AutoRefreshEnabled` | Enables the periodic refresh timer. |
| `AutoRefreshInterval` | Interval between automatic refresh events. |
| `ManualRefreshRequest` | Set to `true` manually to trigger an immediate refresh. The strategy clears it after processing. |
| `RefreshCommandName` | Metadata mirroring the MetaTrader button name; reported in logs when a refresh occurs. |
| `RefreshCommandText` | Metadata mirroring the MetaTrader button caption; reported in logs when a refresh occurs. |
| `TextColorName` | Preserved button text colour description from the MQL script. |
| `BackgroundColorName` | Preserved button background colour description from the MQL script. |

## Usage

1. Configure `CandleType` and `SmaLength` to match the market and timeframe you want to monitor.
2. Enable `AutoRefreshEnabled` and choose `AutoRefreshInterval` if you need scheduled indicator rebuilds. Leave it disabled when you want manual control only.
3. Toggle `ManualRefreshRequest` to `true` whenever you want to flush the indicator state. The flag is automatically set back to `false` once the refresh is registered.
4. Start the strategy to subscribe to market data. It draws candles, the SMA curve, and your own trades on the chart, and it executes the basic SMA trend-following trades once the indicator becomes ready.

## Differences from the Original MQL Script

- StockSharp does not expose chart buttons in the same fashion, so the refresh trigger is implemented through strategy parameters.
- Instead of jumping between M1 and M5 timeframes, the StockSharp port resets its indicators directly, which is more reliable within the framework.
- Button labels and colours are retained as metadata for logging to keep a link to the MetaTrader interface even though no on-chart controls are created.
