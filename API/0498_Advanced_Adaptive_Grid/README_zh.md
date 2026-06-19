# 高级自适应网格策略
[English](README.md) | [Русский](README_ru.md)

高级自适应网格策略利用多种技术指标判断趋势方向，并根据ATR波动性调整网格间距。价格触及网格水平并得到RSI确认时开仓，方向取决于当前趋势。风险控制包含固定止损、止盈、跟踪止损、时间退出以及每日亏损限制。

## 详情

- **入场条件**：
  - 趋势市场：价格到达计算的网格水平并且RSI确认。
  - 震荡市场：RSI超买/超卖触发网格入场。
- **多空方向**：双向。
- **出场条件**：
  - 止损、止盈、跟踪止损、趋势反转或时间到期。
- **止损**：固定与跟踪。
- **默认值**：
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **筛选**：
  - 类别：网格 / 趋势
  - 方向：双向
  - 指标：ATR、SMA、MACD、RSI、Momentum
  - 止损：是
  - 复杂度：高
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：高
