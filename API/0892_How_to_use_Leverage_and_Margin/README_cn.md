# How to Use Leverage and Margin 策略 (中文)
[English](README.md) | [Русский](README_ru.md)

该策略基于随机振荡指标的交叉。当 %K 线在 80 以下上穿 %D 时做多；当 %K 在 20 以上下穿 %D 时做空。持仓使用以 tick 为单位的止盈。

## 细节

- **入场条件**：
  - **多头**：%K 上穿 %D 且 %K < 80。
  - **空头**：%K 下穿 %D 且 %K > 20。
- **多/空**：双向
- **退出条件**：止盈或反向交叉
- **止损**：有，基于 tick 的止盈
- **默认值**：
  - `Stochastic Period` = 13
  - `%K Period` = 4
  - `%D Period` = 3
  - `Take Profit Ticks` = 100
  - `CandleType` = 1 分钟
- **过滤条件**：
  - 类别: Momentum
  - 方向: 双向
  - 指标: Stochastic
  - 止损: 有
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

