# Polarized Fractal Efficiency 策略

该策略基于 **Polarized Fractal Efficiency (PFE)** 指标进行交易。PFE 衡量价格运动的效率，在动量变化时会改变符号。

## 交易逻辑

1. 订阅所选周期的K线并计算 PFE。
2. 如果上一根柱子的 PFE 低于前两根，而当前值高于上一根，则开多单。
3. 如果上一根柱子的 PFE 高于前两根，而当前值低于上一根，则开空单。
4. 在建立新的仓位前先平掉相反方向的仓位。
5. 可选地启用止损和止盈保护。

## 参数

| 名称 | 说明 |
|------|------|
| `CandleType` | 用于分析的K线类型。 |
| `PfePeriod` | PFE 指标的计算周期。 |
| `SignalBar` | 用于生成信号的柱子偏移。 |
| `TakeProfit` | 止盈，价格步数。 |
| `StopLoss` | 止损，价格步数。 |

