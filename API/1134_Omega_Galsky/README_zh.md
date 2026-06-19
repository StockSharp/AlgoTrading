# Omega Galsky
[English](README.md) | [Русский](README_ru.md)

基于 EMA 交叉并带有保本移动止损的策略。

## 详情

- **入场条件**：EMA8 上穿/下穿 EMA21 并结合 EMA89 的价格过滤。
- **多空**：双向。
- **出场条件**：止损、止盈或反向信号。
- **止损**：是。
- **默认参数**：
  - `Ema8Period` = 8
  - `Ema21Period` = 21
  - `Ema89Period` = 89
  - `FixedRiskReward` = 1.0m
  - `SlPercentage` = 0.001m
  - `TpPercentage` = 0.0025m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：EMA
  - 止损：是
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
