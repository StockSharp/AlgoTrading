# Vortex Indicator Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the crossing of positive (VI+) and negative (VI-) lines of the Vortex indicator.
When VI+ crosses above VI- the strategy goes long; when VI- crosses above VI+ it goes short.
A stop loss and take profit in price steps are managed automatically.

## Parameters

- **Vortex Length** – period of the Vortex indicator.
- **Candle Type** – timeframe used for indicator calculation.
- **Stop Loss** – protective stop in price steps.
- **Take Profit** – target profit in price steps.

## Details

- **Indicator**: Vortex
- **Direction**: Long and short
- **Timeframe**: Configurable
- **Risk Management**: Stop loss and take profit via `StartProtection`.
