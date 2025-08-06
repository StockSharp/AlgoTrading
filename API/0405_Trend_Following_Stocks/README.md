# Trend Following Stocks
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades individual equities using a simple trend filter. Stocks trading above a moving average are bought, while those below are avoided or shorted.

The portfolio is refreshed weekly with equal position sizing and trailing stops to protect capital.

## Details

- **Data**: Daily stock closes.
- **Entry**: Buy when price > moving average; short when below.
- **Exit**: Price crosses back through average or stop hit.
- **Instruments**: Liquid equities.
- **Risk**: Trailing stop and position cap.

