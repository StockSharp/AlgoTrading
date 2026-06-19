# ATR 概率指数
[English](README.md) | [Русский](README_ru.md)

基于 ATR 概率指数的策略。

## 详情

- **入场条件**：概率上穿或下穿其移动平均线。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：无。
- **默认参数**：
  - `AtrDistance` = 1.5m
  - `Bars` = 8
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：波动率
  - 方向：双向
  - 指标：ATR、SMA、StandardDeviation
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
