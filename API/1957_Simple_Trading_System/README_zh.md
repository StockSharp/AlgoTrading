# Simple Trading System 策略
[English](README.md) | [Русский](README_ru.md)

该策略复刻自 MetaTrader 的 Simple Trading System。它使用向前偏移的移动平均线，并将当前收盘价与过去的收盘价比较，以捕捉短期趋势反转。当移动平均线低于 `MaShift` 根之前的值，且收盘价位于 `MaShift` 和 `MaPeriod + MaShift` 根之前的收盘价之间，并且当前 K 线为阴线时，生成买入信号。卖出信号则相反。根据参数设置，策略在出现信号时可以开仓和/或平仓多头或空头。可选的止损和止盈参数用于风险控制。

## 细节

- **入场条件**：
  - **做多**：`MA(t) <= MA(t+MaShift)` && `Close(t) >= Close(t+MaShift)` && `Close(t) <= Close(t+MaPeriod+MaShift)` && `Close(t) < Open(t)`
  - **做空**：`MA(t) >= MA(t+MaShift)` && `Close(t) <= Close(t+MaShift)` && `Close(t) >= Close(t+MaPeriod+MaShift)` && `Close(t) > Open(t)`
- **多空方向**：根据 `BuyPositionOpen` 和 `SellPositionOpen` 可同时操作多头和空头。
- **离场条件**：若启用 `BuyPositionClose` 或 `SellPositionClose`，则相反信号会平掉已有仓位。
- **止损/止盈**：可选，通过 `StopLoss` 和 `TakeProfit` 以绝对价格设置。
- **默认值**：
  - `MaType` = EMA
  - `MaPeriod` = 2
  - `MaShift` = 4
  - `PriceType` = Close
  - `CandleType` = 6 小时
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `Volume` = 1
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：移动平均线
  - 止损：支持
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
