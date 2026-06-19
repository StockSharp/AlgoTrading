# Labouchere EA 策略
[English](README.md) | [Русский](README_ru.md)

本策略结合了随机振荡指标交叉与 Labouchere 资金管理序列。当 %K 线与 %D 线交叉时生成买卖信号。Labouchere 系统在每次平仓后调整交易手数：亏损时在序列末尾添加首尾元素之和，盈利时移除首尾元素。

策略仅在已完成的 K 线上执行交易。序列在所有元素被移除后可选择重新开始。时间过滤器允许在指定时段内交易，反向信号可用于平仓。支持以价格步长为单位的固定止损和止盈。

## 细节
- **入场条件**：
  - **做多**：%K 从下向上穿越 %D。
  - **做空**：%K 从上向下穿越 %D。
- **多空方向**：双向。
- **出场条件**：
  - 可选的反向信号平仓。
  - 固定止损和止盈（若设置）。
- **止损**：支持。
- **资金管理**：Labouchere 序列。
- **默认参数**：
  - `LotSequence` = "0.01,0.02,0.01,0.02,0.01,0.01,0.01,0.01"
  - `NewRecycle` = true
  - `StopLoss` = 40
  - `TakeProfit` = 50
  - `IsReversed` = false
  - `UseOppositeExit` = false
  - `UseWorkTime` = false
  - `StartTime` = 00:00
  - `StopTime` = 24:00
  - `KPeriod` = 10
  - `DPeriod` = 190
- **筛选**：
  - 类别：混合
  - 方向：双向
  - 指标：Stochastic Oscillator
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
