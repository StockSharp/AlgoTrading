# Elder Impulse

策略基于Elder的动量系统，将EMA方向与MACD柱状图颜色结合。柱状图为绿色且位于EMA上方时做多，红色且在EMA下方时做空，柱状图变中性则离场。通过融合趋势和动量，该方法力求在强势走势中保持顺势，退出条件简单，只需等待柱状图颜色变化或EMA坡度反转。

## 详情
- **入场条件**: 基于 MACD 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MACD
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
