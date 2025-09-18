# Exp BrainTrend2 AbsolutelyNoLagLwma X2MACandle MMRec 策略

## 概述
该策略把原始 MetaTrader 专家顾问的三段式结构移植到 StockSharp，并使用以下过滤器：

1. **BrainTrend2 思路**：利用 ATR 指标刻画波动率的收缩与扩张，确认趋势背景。
2. **AbsolutelyNoLagLwma 近似**：线性加权移动平均线提供低滞后的方向判断。
3. **X2MACandle 复刻**：快、慢两条 EMA 共同判断 K 线颜色，确认动量。

只有当三个过滤器同时指向同一方向时才开仓。出场由 ATR 驱动的止损与止盈完成，从而模拟原策略中的 MMRec 资金管理模块。

## 交易逻辑
- **做多**：收盘价位于 LWMA 之上且快 EMA 高于慢 EMA。只有在之前的多头信号消失后才允许再次进场，避免重复开仓。
- **做空**：收盘价位于 LWMA 之下且快 EMA 低于慢 EMA。空头信号遵循相同的确认与冷却规则。
- **风险控制**：每根 K 线重新计算 ATR，并据此调整止损与止盈距离。一旦价格触及任一水平，策略会以市价单平仓全部仓位。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 使用的 K 线类型，默认 6 小时，与原始 EA 保持一致。 |
| `AtrPeriod` | ATR 波动率指标的计算周期。 |
| `LwmaLength` | 线性加权移动平均的周期。 |
| `FastMaLength` | 用于判断蜡烛颜色的快 EMA 周期。 |
| `SlowMaLength` | 用于判断蜡烛颜色的慢 EMA 周期。 |
| `StopLossAtrMultiplier` | ATR 止损倍数。 |
| `TakeProfitAtrMultiplier` | ATR 止盈倍数。 |

所有参数均通过 `StrategyParam<T>` 暴露，可在 StockSharp 中直接优化。

## 说明
- 原始 EA 依赖自定义指标缓冲区，本移植版本使用 StockSharp 自带指标实现相同的交易信号。
- 资金管理采用整仓止盈/止损的方式，通过 ATR 动态距离保持 MMRec 的自适应特性。
- 源代码中的注释全部为英文，符合转换规范。
