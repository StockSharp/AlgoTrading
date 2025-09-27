# RSI Strategy
[English](README.md) | [Русский](README_ru.md)

基于 RSI 指标的简单策略。当 RSI 从下向上穿越超卖水平时买入，当 RSI 从上向下穿越超买水平时卖出。

## 详情

- **入场条件**：
  - 多头：RSI 向上穿越 `OverSold`
  - 空头：RSI 向下穿越 `OverBought`
- **多/空**：双向
- **出场条件**：
  - 反向信号
- **止损**：无
- **默认值**：
  - `RsiLength` = 14
  - `OverSold` = 25m
  - `OverBought` = 75m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类别：振荡器
  - 方向：双向
  - 指标：RSI
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
