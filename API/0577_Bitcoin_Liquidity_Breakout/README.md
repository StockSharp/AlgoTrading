# Bitcoin Liquidity Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long positions when liquidity and volatility are high and the short-term trend is bullish. High liquidity is defined as volume above its moving average multiplied by a threshold. Volatility is confirmed when ATR exceeds its moving average.

## Details

- **Entry Criteria**:
  - `Volume > SMA(volume) * LiquidityThreshold`
  - `Price change (%) > PriceChangeThreshold`
  - `Fast SMA > Slow SMA`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **Long/Short**: Long only.
- **Exit Criteria**: Fast SMA crossing below slow SMA or RSI > 70.
- **Stops**: Optional stop-loss and take-profit percentages.
- **Default Values**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: SMA, RSI, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: 1h
