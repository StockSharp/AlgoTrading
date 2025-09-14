# Q2MA Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Q2MA Cross Strategy trades based on the crossover of smoothed moving averages built on candle close and open prices. A long position is opened when the close average falls below the open average after being above, while a short position is opened on the opposite crossover. Positions are closed when an opposite trend appears. The strategy also applies stop loss and take profit levels measured in ticks.

## Details

- **Entry Criteria**: crossover between moving averages of close and open prices
- **Long/Short**: both directions
- **Exit Criteria**: opposite crossover or stop loss/take profit
- **Stops**: yes
- **Default Values**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Moving Average
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: H4
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
