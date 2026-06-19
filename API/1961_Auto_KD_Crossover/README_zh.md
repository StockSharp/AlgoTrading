# Auto KD Crossover Strategy

## 概述
Auto KD Crossover 策略复现了 MQL5 示例 `autoKD_EA`。  
策略使用 `StochasticOscillator` 指标，根据 %K 与 %D 线的交叉生成交易信号。

基础计算使用 RSV 公式：  
`RSV = (收盘价 - N 期最低价) / (N 期最高价 - N 期最低价) * 100`  
其中最高价与最低价在 `KDPeriod` 根K线内求得。%K 为 RSV 的移动平均，周期为 `KPeriod`；%D 为 %K 的移动平均，周期为 `DPeriod`。

## 参数
| 名称 | 说明 | 默认值 |
|------|------|--------|
| `KDPeriod` | 计算 RSV 的基础周期数。 | 30 |
| `KPeriod` | %K 线的平滑周期。 | 3 |
| `DPeriod` | %D 线的平滑周期。 | 6 |
| `CandleType` | 使用的K线类型与周期。 | 5 分钟 |
| `Volume` | 继承自 `Strategy` 的下单数量。 | `Strategy.Volume` |

所有参数均可用于优化。

## 交易逻辑
1. 订阅指定的K线并计算随机指标。
2. 当上一根K线的 %K 低于 %D 而当前 %K 向上穿越 %D 时，开多。
3. 当上一根K线的 %K 高于 %D 而当前 %K 向下穿越 %D 时，开空。
4. 策略一次仅持有一个方向的仓位。反向交叉将平掉当前仓位并开立相反仓位。
5. `StartProtection()` 调用启用 StockSharp 提供的默认风控机制。

## 可视化
策略自动在图表上绘制K线、随机指标及成交点位。

## 备注
- 可用于任意品种和时间框架。
- 请根据市场波动性调整参数。
