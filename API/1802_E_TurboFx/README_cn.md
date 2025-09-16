# E TurboFx 策略
[English](README.md) | [Русский](README_ru.md)

基于 MQL5 专家顾问“e-TurboFx”的 StockSharp 实现。策略监控同方向蜡烛实体逐渐增大的序列。当出现多根实体不断增大的阴线时买入，预期价格反转；当出现多根实体不断增大的阳线时卖出。止损和止盈以价格点数形式设置，可选。

## 细节

- **入场条件**：
  - 多头：连续 `N` 根阴线且每根实体都大于前一根
  - 空头：连续 `N` 根阳线且每根实体都大于前一根
- **多空方向**：双向
- **离场条件**：止损或止盈
- **止损/止盈**：通过 `StartProtection` 以点数设置
- **默认值**：
  - `BarsCount` = 3
  - `StopLossPoints` = 700
  - `TakeProfitPoints` = 1200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 分类：Price Action
  - 方向：双向
  - 指标：无
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等
