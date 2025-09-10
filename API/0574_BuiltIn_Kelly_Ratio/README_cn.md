# 内置凯利比例
[English](README.md) | [Русский](README_ru.md)

该策略使用移动平均线与ATR通道，并根据凯利比例动态调整仓位。

## 详情

- **入场条件**: 价格向上或向下突破ATR通道。
- **多空方向**: 双向。
- **退出条件**: 可选的止盈与止损。
- **止损**: 可选。
- **默认值**:
  - `Length` = 20
  - `Multiplier` = 1
  - `AtrLength` = 10
  - `UseEma` = true
  - `UseKelly` = true
  - `UseTakeProfit` = false
  - `UseStopLoss` = false
  - `TakeProfit` = 10
  - `StopLoss` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: MA, ATR
  - 止损: 可选
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
