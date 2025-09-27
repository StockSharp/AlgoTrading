# 期货吞没蜡烛大小策略
[English](README.md) | [Русский](README_ru.md)

当蜡烛的高低差在指定时间窗口内超过设定的tick阈值时，每天交易一次。按照蜡烛方向入场，并使用止盈和止损退出。

## 详情

- **入场条件**：在交易时段内蜡烛范围达到tick阈值。
- **多/空**：双向。
- **出场条件**：止盈或止损。
- **止损**：止盈和止损。
- **默认值**：
  - `CandleType` = 1 分钟
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **过滤器**：
  - 分类: 模式
  - 方向: 双向
  - 指标: K线
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
