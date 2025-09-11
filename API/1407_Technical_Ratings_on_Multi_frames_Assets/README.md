# Technical Ratings on Multi-frames Assets Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy aggregates technical ratings from multiple time frames.
It compares price against a moving average and RSI thresholds on 1h, 4h, and daily candles.
A long position is opened when the combined rating is positive and a short position when it is negative.

## Details

- **Entry**: Buy when average rating > 0; sell when average rating < 0.
- **Indicators**: SMA, RSI.
- **Timeframes**: 1h, 4h, 1d.
- **Type**: Trend-following.
- **Stops**: None.
- **Direction**: Long & Short.
- **Risk**: Medium.
- **Complexity**: Medium.
