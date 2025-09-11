# Correlation Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements John Ehlers' Correlation Cycle, Correlation Angle and Market State concepts. It computes the correlation between price and sine/cosine components to obtain a correlation angle. When the angle is stable and above zero the strategy enters a long position. When the angle is stable and below zero it enters a short position.

## Parameters
- Candle Type
- Period
- Market State Threshold
