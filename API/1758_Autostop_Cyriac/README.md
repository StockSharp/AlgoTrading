# Autostop Cyriac Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy automatically attaches a take profit and stop loss to each trade while it is active. It does not create entries or exits itself and can be combined with manual trading or other strategies.

## Details

- **Entry Criteria**: None. Positions are opened manually or by external logic.
- **Long/Short**: Both long and short positions are supported.
- **Exit Criteria**: Positions are closed by the attached take profit or stop loss.
- **Stops**: Yes. Absolute price offsets for both take profit and stop loss via `StartProtection`.
- **Filters**: None.

The strategy exposes two parameters:

- `TakeProfit` – distance to the take profit in price units.
- `StopLoss` – distance to the stop loss in price units.
