# Back to the Future 策略
[English](README.md) | [Русский](README_ru.md)

该动量策略比较当前收盘价与若干分钟前的价格。当价格相对于历史价格上升超过设定阈值时，系统开多单；当价格低于负阈值时，系统开空单。理念是价格从过去水平大幅偏离通常意味着趋势正在形成。

策略仅在完成的K线基础上运行，可用于 StockSharp 支持的任何品种和时间框架。开仓后，通过固定的止盈和止损来控制风险。过去价格通过一个队列维护，用于计算价格差。

## 细节

- **入场条件**：
  - **多头**：`Close(t) - Close(t-Δ) > BarSize`。
  - **空头**：`Close(t) - Close(t-Δ) < -BarSize`。
- **多/空**：双向。
- **出场条件**：
  - **多头**：`Close >= Entry + TakeProfit` 或 `Close <= Entry - StopLoss`。
  - **空头**：`Close <= Entry - TakeProfit` 或 `Close >= Entry + StopLoss`。
- **止损**：是，使用价格单位表示的固定止盈和止损。
- **默认值**：
  - `BarSize = 0.25`
  - `HistoryMinutes = 60`
  - `TakeProfit = 10`
  - `StopLoss = 5000`
- **过滤器**：
  - 分类: 趋势跟随
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 简单
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
