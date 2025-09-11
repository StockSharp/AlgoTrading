# Martingale with MACD and KDJ Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters trades when both the MACD line and the KDJ %K line cross their signal lines in the same direction. It pyramids positions using a martingale approach, adding when price moves against the trade by a configured percent and then rebounds.

Positions are closed when a take profit, stop loss, or trailing stop condition is met.

## Details

- **Entry**: MACD line and KDJ %K line cross their signal lines in the same direction.
- **Additions**: Up to `Max Additions` times when price moves by `Add Position Percent` and rebounds by `Rebound Percent`. Each addition size is multiplied by `Add Multiplier`.
- **Exit**: Close on `Take Profit Trigger`, `Stop Loss Percent` or trailing stop hit.
- **Direction**: Long and short.

