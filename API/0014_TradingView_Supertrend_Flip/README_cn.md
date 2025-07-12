# TradingView Supertrend 翻转

策略模拟流行的Supertrend指标颜色翻转，结合成交量确认。指标从红转绿时做多，从绿转红时做空，下一次翻转即平仓。使用成交量过滤可在交易清淡时避免震荡，只在有量支撑的翻转中入场，力求捕捉更可靠的反转。

## 详情
- **入场条件**: 基于 ATR、Supertrend 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, Supertrend
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
