# Game Theory Trading Strategy
[English](README.md) | [Русский](README_ru.md)

Game Theory Trading Strategy 结合了群体行为分析、流动性陷阱侦测、机构资金流和纳什均衡区间，用于反向与动量交易。

策略监控 RSI 极值和成交量激增以识别群体买入或卖出。近期高低点附近的流动性陷阱、AD 指标和「聪明资金」偏向共同优化进场。基于均线和标准差的价格带定义纳什均衡用于回归交易。当价格接近均衡或出现机构成交量时，仓位大小会自动调整。

## 细节
- **数据**: 价格与成交量 K 线。
- **入场条件**: 反向、动量或纳什回归信号。
- **离场条件**: 止损/止盈或相反信号。
- **止损**: 可选的止损和止盈。
- **默认参数**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **过滤器**:
  - 类型: 反向与动量混合
  - 方向: 多空皆可
  - 指标: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - 复杂度: 高级
  - 风险等级: 中等
