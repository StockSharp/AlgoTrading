# 昨日高点策略
[English](README.md) | [Русский](README_ru.md)

多头突破策略，在昨日高点上方下达买入止损单。
可选的ROC滤波、跟踪止损和EMA平仓提供风险控制。

## 细节

- **入场条件**：收盘价低于昨日高点，然后在高点+gap处挂买入止损
- **多空方向**：仅做多
- **离场条件**：止损、止盈、可选跟踪止损或EMA交叉
- **止损**：是，基于百分比
- **默认值**：
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 分钟
- **过滤器**：
  - 分类: 突破
  - 方向: 多头
  - 指标: 价格、ROC、EMA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
