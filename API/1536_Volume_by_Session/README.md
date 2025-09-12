# Volume by Session Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified strategy derived from the TradingView **"Volume by Session"** indicator. The trading day is divided into four sessions, each with its own average volume. When current volume within a session deviates from its average, the strategy enters trades accordingly.

## Details

- **Entry**: Current session volume above or below its moving average.
- **Exit**: Opposite signal closes existing position.
- **Long/Short**: Both.
- **Indicators**: SMA.
- **Timeframe**: Intraday.

This is a minimal educational translation; visualization and extensive settings of the original script are omitted.
