# Volume and Volatility Ratio Indicator WODI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified strategy derived from the TradingView script **"Volume and Volatility Ratio Indicator - WODI"**. It monitors the product of volume and price volatility to spot potential reversals. When the volatility index exceeds a dynamic threshold and the recent candles show a change in direction, the strategy opens a position with Fibonacci-based risk management.

## Details

- **Entry**: High volume and volatility together with candle reversal pattern.
- **Exit**: Stop loss and take profit calculated from candle range and Fibonacci multipliers.
- **Long/Short**: Both.
- **Timeframe**: Any.
- **Indicators**: SMA.

This is a simplified educational port. Original TradingView logic is reduced.
