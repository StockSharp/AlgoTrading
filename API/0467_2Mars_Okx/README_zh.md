# 2Mars OKX 策略

该策略结合移动平均线交叉与 SuperTrend 过滤。布林带用于设定获利目标，ATR 止损限制风险。

## 规则
- **多头**：信号 EMA 上穿基准 EMA 且价格位于 SuperTrend 之上。
- **空头**：信号 EMA 下穿基准 EMA 且价格位于 SuperTrend 之下。
- **离场**：价格触及布林带上/下轨获利，或 ATR 乘以系数触发止损。

## 指标
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
