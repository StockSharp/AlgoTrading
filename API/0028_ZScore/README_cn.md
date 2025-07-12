# ZScore

该均值回归策略基于Z分数指标，衡量价格相对均线的偏离。当z值过高或过低时，表明价格过度延伸，采取逆势交易，z值回归正常后平仓。ZScore适用于任何时间序列，配合波动调整的退出机制能适应多变的市场环境。

## 详情
- **入场条件**: 基于 MA、ZScore 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 均值回归
  - 方向: 双向
  - 指标: MA, ZScore
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
