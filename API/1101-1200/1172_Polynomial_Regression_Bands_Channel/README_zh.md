# Polynomial Regression Bands Channel 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略对最近价格进行多项式回归，并根据残差的标准差构建上下通道。当价格跌破下轨时开多头仓位，当价格突破上轨时开空头仓位。

## 细节

- **入场条件**:
  - **多头**: `Close < LowerBand`。
  - **空头**: `Close > UpperBand`。
- **多空方向**: 双向。
- **出场条件**:
  - 相反信号。
- **止损**: 否。
- **默认值**:
  - `Length` = 100。
  - `Degree` = 2。
  - `StdDevMultiplier` = 2。
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **筛选器**:
  - 类别: 均值回归
  - 方向: 双向
  - 指标: 多项式回归
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

## 计算与执行

每根已完成蜡烛在 -1 到 1 的等距网格上，对最近 `Length` 个收盘价（包含当前收盘价）进行 `Degree` 次多项式最小二乘拟合。通道中心为最新观测点的拟合值。宽度为 `StdDevMultiplier * sqrt(sum((close[i] - fitted[i])^2) / Length)`，即残差的总体标准差，不做自由度修正。

收盘价严格低于下轨时做多，严格高于上轨时做空。反向入场以一笔数量为 `Volume + abs(Position)` 的市价订单完全反转仓位；空仓入场使用 `Volume`。价格位于通道内时保持仓位。窗口未满不交易，没有冷却时间或保护止损。

有效设置：`0 <= Degree <= 6`、`Length >= Degree + 2`、正的 `StdDevMultiplier`。零次拟合为常数。固定归一化网格预计算经两次正交化的标准正交多项式基；每个窗口以 double 精度投影价格偏移。C# 与 Python 使用相同计算，订单数量保持 decimal。
