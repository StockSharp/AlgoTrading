# 20/200 Expert 策略
[English](README.md) | [Русский](README_ru.md)

该策略根据两个历史K线的开盘价差来开仓。当 shift2 的开盘价减去 shift1 的开盘价超过阈值时做多，反之做空。仅在指定小时开仓，并通过止盈、止损或超过最大持仓时间来平仓。

## 细节

- **入场条件：**
  - 多头：open[Shift2] - open[Shift1] > DeltaLong 点。
  - 空头：open[Shift1] - open[Shift2] > DeltaShort 点。
- **多空方向：** 双向。
- **出场条件：** 止盈、止损或最大持仓时间。
- **止损：** 固定的点数止损和止盈。
- **默认值：**
  - Shift1 = 6
  - Shift2 = 2
  - DeltaLong = 6 点
  - DeltaShort = 21 点
  - TakeProfitLong = 390 点
  - StopLossLong = 1470 点
  - TakeProfitShort = 320 点
  - StopLossShort = 2670 点
  - TradeHour = 14
  - MaxOpenTime = 504 小时
  - Volume = 0.1
  - K线周期 = 1 小时
- **过滤器：**
  - 类别：动量
  - 方向：多空
  - 指标：无
  - 止损：有
  - 复杂度：中等
  - 时间框架：小时
  - 季节性：按时间过滤
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
