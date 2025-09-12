# Volume Block Order Analyzer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified strategy based on the TradingView script **"Volume Block Order Analyzer"**. It measures how large volume spikes impact price direction and accumulates this effect over time. When the cumulative impact crosses user-defined thresholds, the strategy enters trades and protects them with a trailing stop.

## Details

- **Entry**: Cumulative impact above or below threshold.
- **Exit**: Trailing stop based on percentage from entry.
- **Long/Short**: Both.
- **Indicators**: SMA.
- **Timeframe**: Any.

This port focuses on the core idea; many visual features of the original script are omitted.
