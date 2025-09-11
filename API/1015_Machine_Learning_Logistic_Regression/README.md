# Machine Learning Logistic Regression Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy retrains a simple logistic regression model on every bar.
The model uses recent closing prices and a synthetic series derived from them.
If the predicted probability of growth is above 0.5 the strategy enters a long position; otherwise it goes short.
Positions are held for a fixed number of bars.

## Details
- **Entry**: prediction > 0.5 → long, otherwise short.
- **Exit**: opposite signal or holding period reached.
- **Long/Short**: both.
- **Timeframe**: configurable, default 1 minute.
