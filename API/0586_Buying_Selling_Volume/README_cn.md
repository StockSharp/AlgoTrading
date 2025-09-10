# 买卖量策略
[English](README.md) | [Русский](README_ru.md)

该策略利用买卖量分布来判断行情。
当买入量占优且体量指标突破波动带并且价格在周VWAP之上时开多。
当卖出量占优且指标突破上轨并且价格在周VWAP之下时开空。

## 细节

- **入场条件**：
  - **多头**：买入量 > 卖出量，指标在上轨之上，收盘价在周VWAP之上。
  - **空头**：卖出量 > 买入量，指标在上轨之上，收盘价在周VWAP之下。
- **多/空**：均可。
- **出场条件**：反向信号或基于ATR的止盈止损。
- **止损**：通过 `ProfitTargetLong`, `StopLossLong`, `ProfitTargetShort`, `StopLossShort` 设置的 ATR 百分比。
- **默认值**：
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **过滤器**：
  - 类别：成交量
  - 方向：双向
  - 指标：自定义
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
