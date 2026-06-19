# 网格机器人回测策略
[English](README.md) | [Русский](README_ru.md)

该策略在预设价格区间内构建网格，当价格跌至网格线时买入，在价格上升到下一条线时卖出。网格边界可手动设定或根据近期数据自动计算。

## 细节

- **入场条件**：
  - **多头**：价格跌破没有持仓的网格线
- **多空方向**：仅多头
- **出场条件**：
  - 价格突破下一条网格线
- **止损**：无
- **默认值**：
  - `AutoBounds` = true
  - `BoundSource` = "Hi & Low"
  - `BoundLookback` = 250
  - `BoundDeviation` = 0.10
  - `UpperBound` = 0.285
  - `LowerBound` = 0.225
  - `GridLines` = 30
- **过滤器**：
  - 类别：区间交易
  - 方向：多头
  - 指标：Highest、Lowest、SimpleMovingAverage
  - 止损：否
  - 复杂度：中
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
