# Black-Scholes Delta Hedge
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy computes the theoretical price and delta of an option using the Black-Scholes model. At specified intervals it hedges the delta by trading the underlying asset.

## Details
- **Function**: Delta hedging using Black-Scholes pricing
- **Parameters**: Strike Price, Risk Free Rate, Volatility, Days To Expiry, Option Type, Position Side, Position Size, Hedge Interval, Candle Type
- **Indicators**: None
- **Long/Short**: Depends on position side
- **Stops**: None
