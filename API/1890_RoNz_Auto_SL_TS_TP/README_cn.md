# RoNz Auto SL TS TP 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于EMA交叉开仓，并自动管理止损和止盈。  
入场后立即设置初始止损与目标，随后可选择锁定利润并启动移动止损。

## 详情

- **入场条件**：
  - 多头：`EMA10 < EMA20 && EMA10 > EMA100`
  - 空头：`EMA10 > EMA20 && EMA10 < EMA100`
- **多/空**：双向
- **离场条件**：止损、止盈、利润锁定或移动止损
- **止损**：有
- **默认值**：
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **筛选**：
  - 类别：风险管理
  - 方向：双向
  - 指标：EMA
  - 止损：SL/TP/Trailing
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
