# YinYang RSI Volume Trend 策略
[English](README.md) | [Русский](README_ru.md)

YinYang RSI Volume Trend 策略利用成交量加权的价格区域和 RSI 过滤器来捕捉趋势反转。当价格离开下方区域时买入，离开上方区域时卖出。可选的止损和止盈基于动态区域计算。

## 详情

- **入场条件**：价格穿越计算得到的买入区域，并依据重置模式确认可用性。
- **多/空**：双向。
- **离场条件**：价格到达相反区域或触发可选的止损/止盈。
- **止损**：可选。
- **默认参数**：
  - `TrendLength` = 80
  - `UseTakeProfit` = true
  - `UseStopLoss` = true
  - `StopLossMultiplier` = 0.1
- **过滤器**：
  - 类别: Trend
  - 方向: 双向
  - 指标: VWMA, EMA, RSI
  - 止损: 可选
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
