# Elder Impulse System Strategy
[English](README.md) | [Русский](README_ru.md)

该策略实现了 Elder 冲动系统，将指数移动平均线 (EMA) 的方向与 MACD 柱状图的动量结合。在较高级别的时间框架上，当看涨或看跌冲动减弱时开仓。

方法基于 EMA 斜率和 MACD 柱状图变化得到的颜色信号：
- **绿色 (2)** — EMA 上升且 MACD 柱状图为正并上升。
- **红色 (1)** — EMA 下降且 MACD 柱状图为负并下降。
- **蓝色 (0)** — 其他情况。

当之前的看涨冲动减弱时开多单，之前的看跌冲动减弱时开空单。出现相反冲动时平掉相应仓位。

## 详情

- **入场条件**：已完成的蜡烛上 Elder Impulse 颜色变化。
- **多/空**：双向。
- **出场条件**：相反冲动或仓位保护。
- **止损**：默认使用 `StartProtection`，止损和止盈均为 2%。
- **默认值**：
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：EMA，MACD
  - 止损：是
  - 复杂度：中等
  - 时间框架：4 小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
