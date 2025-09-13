# EMA SAR Bulls Bears Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a fast and slow Exponential Moving Average (EMA), Parabolic SAR, and Bulls/Bears Power indicators. It trades only during a configured intraday window and uses simple profit and loss protections.

A short position opens when EMA3 is below EMA34, the Parabolic SAR is above the candle high, and Bears Power is negative but rising. A long position opens when EMA3 is above EMA34, SAR is below the candle low, and Bulls Power is positive but falling.

## Details

- **Entry Criteria**:
  - **Long**: EMA3 above EMA34, SAR below the candle low, Bulls Power > 0 and decreasing.
  - **Short**: EMA3 below EMA34, SAR above the candle high, Bears Power < 0 and increasing.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or triggered stop/take.
- **Stops**: Yes, absolute take-profit (400 points) and stop-loss (2000 points).
- **Filters**:
  - Trades only between 09:00 and 17:00.
  - Operates on 15-minute candles.
