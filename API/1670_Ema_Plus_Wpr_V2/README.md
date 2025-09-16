# EMA plus WPR v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Williams %R oscillator with EMA trend filter. Trades when WPR reaches extreme levels after a retracement. Includes optional WPR-based exits, trailing stops and bar-based exit.

## Details

- **Long**: WPR hits -100 after retracement and EMA trend is up.
- **Short**: WPR hits 0 after retracement and EMA trend is down.
- **Indicators**: Williams %R, EMA.
- **Stops**: fixed stop loss and take profit, optional trailing stop.
