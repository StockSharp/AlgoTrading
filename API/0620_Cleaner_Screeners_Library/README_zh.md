# Cleaner Screeners 库
[English](README.md) | [Русский](README_ru.md)

简单的筛选器策略，对多个品种的 RSI 进行评估并输出买入或卖出信号。可作为构建自定义多资产筛选器的基础。

## 细节

- **入场条件**：对每个品种的 RSI 数值与阈值比较。
- **方向**：无（仅信号）
- **出场条件**：无
- **止损**：无
- **默认值**：
  - `RsiLength` = 14
  - `StrongThreshold` = 70m
  - `WeakThreshold` = 60m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类别：筛选器
  - 方向：N/A
  - 指标：RSI
  - 止损：否
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：N/A
