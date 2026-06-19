# NRTR Extr 策略

该策略实现了带有附加信号箭头的 **Nick Rypock Trailing Reverse** (NRTR) 算法，是 MQL5 示例“Exp_NRTR_extr”的 StockSharp 高级 API 版本。

## 工作原理

- 自定义的 `NrtrExtrIndicator` 根据设定周期计算平均波动并绘制跟踪水平。
- 当价格突破该水平时，指标会改变方向并发出买入或卖出信号。
- 策略在买入信号时开多单，在卖出信号时开空单。
- 已有仓位在出现反向信号或达到止损/止盈水平时平仓。

## 参数

| 名称 | 说明 |
| --- | --- |
| `Period` | 用于计算平均波动的K线数量。 |
| `Digits Shift` | 对波动系数的额外精度调整。 |
| `Stop Loss` | 价格点数表示的止损距离。 |
| `Take Profit` | 价格点数表示的止盈目标。 |
| `Enable Buy Open` / `Enable Sell Open` | 允许开多或开空。 |
| `Enable Buy Close` / `Enable Sell Close` | 允许在反向信号时平仓。 |
| `Candle Type` | 指标使用的K线时间框架。 |

## 说明

指标基于平均真实波幅 (ATR) 来评估市场波动性。策略在图表区域自动绘制K线和成交交易以便观察。

