# Calculation Position Size Based on Risk 策略
[English](README.md) | [Русский](README_ru.md)

演示如何根据账户风险和止损百分比计算仓位大小。为了展示计算逻辑，入场是随机的。

## 细节

- **入场条件**：
  - **多头**：每第 333 根K线。
  - **空头**：每第 444 根K线。
- **多空方向**：双向。
- **出场条件**：
  - 仅止损。
- **止损**：止损。
- **默认值**：
  - `Stop Loss %` = 10
  - `Risk Value` = 2
  - `Risk Is Percent` = true
  - `Long Period` = 333
  - `Short Period` = 444
- **筛选**：
  - 类别: Risk Management
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 低
