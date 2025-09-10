# Binomial Option Pricing Model
[Русский](README_ru.md) | [中文](README_cn.md)

This module calculates the theoretical price of an option using a two-step binomial tree. It supports American or European styles and call or put types across different asset classes. Volatility is estimated through the standard deviation of closing prices.

No trading signals are produced; the strategy logs the calculated option price for each finished candle.

## Details
- **Function**: Option pricing (no trades)
- **Parameters**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **Indicators**: Standard Deviation
- **Long/Short**: N/A
- **Stops**: None
