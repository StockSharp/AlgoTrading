# Exp CyclePeriod Strategy
[English](README.md) | [Русский](README_ru.md)

该策略使用 CyclePeriod 指标来识别市场周期的转折。当指标上升时开多仓，当指标下降时开空仓，并在指标反向时平掉相反持仓。

## 详情

- **入场条件：**
  - **多头**：CyclePeriod 上升且当前值高于前一值。
  - **空头**：CyclePeriod 下降且当前值低于前一值。
- **多/空方向**：多头和空头。
- **出场条件：**
  - 当 CyclePeriod 向上转折时平掉空单。
  - 当 CyclePeriod 向下转折时平掉多单。
- **止损止盈**：使用以价格单位表示的止盈和止损。
- **默认参数：**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true。
  - `SellPosClose` = true。
- **过滤器：**
  - 类别：趋势跟随
  - 方向：多头和空头
  - 指标：CyclePeriod
  - 止损：是
  - 复杂度：中等
  - 时间框架：6 小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
