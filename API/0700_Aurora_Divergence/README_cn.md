# Aurora Divergence 策略

该策略利用价格与累积能量（OBV）的背离进行交易，通过比较价格与 OBV 的线性回归斜率来捕捉潜在反转。

## 主要特性

- 线性回归斜率比较用于检测背离信号。
- 可选的 z-score 过滤器避免在极端价格进入。
- 通过更高周期均线确认趋势方向。
- 基于 ATR 的波动性阈值和风险管理，动态设置止损和目标。
- 每次交易后有冷却期，并限制持仓时间。

## 参数

| 名称 | 说明 |
|------|------|
| `CandleType` | 主计算的 K 线周期。 |
| `Lookback` | 斜率计算周期。 |
| `ZLength` | z-score 计算的均值和标准差周期。 |
| `ZThreshold` | 允许进场的最大 z-score 绝对值。 |
| `UseZFilter` | 是否启用 z-score 过滤。 |
| `HtfCandleType` | 趋势均线的高周期。 |
| `HtfMaLength` | 高周期均线长度。 |
| `AtrLength` | ATR 周期，用于波动性和风险。 |
| `AtrThreshold` | 允许交易的最小 ATR 值。 |
| `StopAtrMultiplier` | 止损距离的 ATR 倍数。 |
| `ProfitAtrMultiplier` | 止盈距离的 ATR 倍数。 |
| `MaxBarsInTrade` | 持仓的最大 K 线数量。 |
| `CooldownBars` | 交易后的冷却期 K 线数量。 |

## 复杂度

中级

