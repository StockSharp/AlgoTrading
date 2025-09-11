# 黄金和欧元/美元流动性抓取策略
[English](README.md) | [Русский](README_ru.md)

该策略利用RSI、SMA、随机指标和基于ATR的公平价值缺口，在黄金和欧元/美元的供需区寻找流动性抓取。

## 细节

- **入场条件**：
  - **做多**：价格下破最近低点形成影线，市场结构上移，出现FVG，RSI超卖，价格高于SMA，随机指标超卖。
  - **做空**：价格上破最近高点形成影线，市场结构下移，出现FVG，RSI超买，价格低于SMA，随机指标超买。
- **多空**：双向。
- **退出**：反向信号。
- **止损**：无。
- **默认值**：
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **过滤器**：
  - 类别：价格行为
  - 方向：双向
  - 指标：RSI、SMA、Stochastic、ATR、Highest、Lowest
  - 止损：无
  - 复杂度：中等
  - 时间框：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
