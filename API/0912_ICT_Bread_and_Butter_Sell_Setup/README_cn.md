# ICT Bread and Butter Sell-Setup 策略
[English](README.md) | [Русский](README_ru.md)

该策略跟踪伦敦、纽约和亚洲交易时段的高点和低点，并根据预定义条件进行交易。

## 细节

- **入场条件**：
  - **纽约做空**：价格在纽约时段创下高于伦敦高点的新高且收出看跌蜡烛。
  - **伦敦收盘买入**：10:30 到 13:00 之间价格收于伦敦低点之下。
  - **亚洲做空**：亚洲时段价格收于亚洲高点之上。
- **多空方向**：双向。
- **出场条件**：
  - 每笔交易使用以跳动为单位的止损和止盈。
- **止损**：是。
- **默认值**：
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **筛选**：
  - 类别：Price action
  - 方向：双向
  - 指标：Price action
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等

