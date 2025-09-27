# Quatro SMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines three fast simple moving averages (SMAs) with a long-term SMA and a volume filter. A long position opens when the fastest SMA is above the middle SMA, the middle is above the slow SMA, price is above the long SMA, and volume exceeds its average by a configurable multiplier. Short positions require the opposite alignment.

The position exits in several stages: up to three take-profit levels and a stop-loss can close portions of the trade. A reverse SMA alignment also closes the position.

## Details

- **Indicators**: SMA, Volume
- **Timeframe**: 4h
- **Type**: Trend following with volume confirmation
- **Stops**: Three take-profit levels and one stop-loss
