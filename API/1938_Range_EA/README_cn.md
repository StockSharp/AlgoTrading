# Range EA 策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格偏离移动平均线到达固定范围时进行交易。当价格向上或向下偏离到指定距离时开仓。支持可选的追踪止损、分步加仓、反转模块和交易时间过滤。

## 细节

- **入场条件**：
  - 多头：收盘价高于移动平均线 + `Range`
  - 空头：收盘价低于移动平均线 - `Range`
- **方向**：双向
- **出场条件**：
  - 触及 `TakeProfit` 或 `StopLoss`
  - 启用时触发追踪止损
  - 可选的在移动 `Turn` 后反转
- **止损**：固定值
- **默认值**：
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：Range
  - 方向：双向
  - 指标：MA
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
