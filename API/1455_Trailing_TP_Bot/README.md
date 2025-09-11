# Trailing TP Bot
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades SMA crossovers on both long and short sides. Each position defines a fixed take profit and stop loss. After the profit target is hit the stop can trail to protect gains.

## Details

- **Entry**: Fast SMA crosses slow SMA.
- **Exit**: Stop loss, take profit or trailing stop.
- **Indicators**: SMA.
- **Direction**: Both.
- **Risk**: Fixed stop loss with optional trailing.
