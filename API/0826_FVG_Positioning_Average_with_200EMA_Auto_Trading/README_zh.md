# FVG Positioning Average with 200EMA Auto Trading 策略
[English](README.md) | [Русский](README_ru.md)

该策略对多头和空头公平价值缺口（FVG）的水平进行平均，并结合200周期EMA。当价格沿趋势方向突破这些平均值时开仓。

## 详情

- **入场条件**：
  - **做多**：价格向上突破空头FVG平均值，且所有平均值高于EMA。
  - **做空**：价格向下突破多头FVG平均值，且所有平均值低于EMA。
- **多空方向**：双向。
- **出场条件**：
  - 在最近的低点/高点设置止损。
  - 按风险回报比设置止盈。
- **止损**：是。
- **默认值**：
  - `FvgLookback` = 30
  - `AtrMultiplier` = 0.25
  - `LookbackPeriod` = 20
  - `EmaPeriod` = 200
  - `RiskReward` = 1.5
- **过滤器**：
  - 类别：价格行为
  - 方向：双向
  - 指标：ATR、EMA、SMA、Highest、Lowest
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
