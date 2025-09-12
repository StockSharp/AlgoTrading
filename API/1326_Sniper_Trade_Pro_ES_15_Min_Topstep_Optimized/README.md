# Sniper Trade Pro Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades the E-mini S&P (ES) on 15-minute candles during the New York morning session. It combines 9/21 EMA trend confirmation, VWAP, money flow divergence and ADX with bullish and bearish engulfing patterns. Position size is derived from ATR-based risk per trade, using a 0.8 ATR stop and 2 ATR target, with stop moved to break-even after 1 ATR profit. Trading stops for the day after a $1,000 loss.
