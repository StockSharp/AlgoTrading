# 零滞后波动突破EMA趋势策略
[English](README.md) | [Русский](README_ru.md)

该策略利用零滞后EMA差值与布林带，结合EMA趋势过滤器，在波动性突破时进场，可选择持有至反向信号。

## 详情

- **入场条件**：dif 上穿上轨且 EMA 斜率确认。
- **多空方向**：双向。
- **退出条件**：可选的中轨下穿退出。
- **止损**：无显式止损。
- **默认值**：
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: EMA, Bollinger Bands
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
