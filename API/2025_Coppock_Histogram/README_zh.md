# Coppock Histogram 策略
[English](README.md) | [Русский](README_ru.md)

该策略交易 Coppock Histogram 指标的转向。指标把两个变动率数值相加，并使用移动平均进行平滑。当动量向上转折时，策略开多单并平仓空单；动量向下转折时，策略平仓多单并开空单。只在蜡烛完全形成后评估信号。

## 细节

- **入场条件**：Coppock Histogram 向上倾斜买入，向下倾斜卖出。
- **多/空**：双向。
- **出场条件**：相反信号平掉当前仓位。
- **止损**：默认没有止损或止盈。
- **默认值**：
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **过滤器**：
  - 分类：振荡指标
  - 方向：双向
  - 指标：RateOfChange, SimpleMovingAverage
  - 止损：无
  - 复杂度：基础
  - 时间框架：8小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
