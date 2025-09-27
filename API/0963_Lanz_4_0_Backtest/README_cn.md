# LANZ Strategy 4.0 Backtest
[English](README.md) | [Русский](README_ru.md)

LANZ Strategy 4.0 Backtest 是一种利用摆动枢轴检测趋势的突破策略。当价格突破最后一个枢轴高点时做多，跌破最后一个枢轴低点时做空。仓位大小根据账户权益、风险百分比和点值计算，止损设在最近摆动点加缓冲，止盈按风险收益比计算。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **多头**: 价格突破最后枢轴高点。
  - **空头**: 价格跌破最后枢轴低点。
- **出场条件**: 止损或止盈。
- **止损**: 最近摆动点加缓冲。
- **默认参数**:
  - `SwingLength` = 180
  - `SlBufferPoints` = 50
  - `RiskReward` = 1
  - `RiskPercent` = 1
  - `PipValueUsd` = 10
- **过滤器**:
  - 类别: 突破
  - 方向: 多头 & 空头
  - 指标: Highest, Lowest
  - 复杂度: 中等
  - 风险等级: 中等
