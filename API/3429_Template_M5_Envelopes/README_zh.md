# Template M5 Envelopes 策略

本策略由 MetaTrader 4 专家顾问 “Template_M5_Envelopes.mq4” 转换而来。它在 5 分钟 K 线图上计算线性加权移动平均线（LWMA）包络通道，当价格远离通道足够距离时布置突破型止损挂单。挂单会随着市场自动调价，成交后还会根据参数设置挂出止损、止盈以及可选的跟踪止损。

## 交易逻辑

1. 使用 `EnvelopePeriod` 指定的长度对蜡烛的中位价（(High+Low)/2）计算 LWMA，并按照 `EnvelopeDeviation` 百分比得到上下包络带。
2. 每根收完的 5 分钟蜡烛都会保存自身的包络数值以及最高价和最低价。只有当“上一根”蜡烛的这些数据全部可用时才会生成信号，与原版 EA 使用 `iEnvelopes(..., shift = 1)` 的行为一致。
3. 当满足以下条件时触发**买入**设置：
   * 上一根蜡烛的最低价至少低于上一根下包络 `DistancePoints` 点；
   * 当前买价也至少低于同一包络 `DistancePoints` 点。
4. **卖出**设置与之对称，使用上一根蜡烛的最高价与上包络进行比较。
5. 每次仅允许存在一个挂单或持仓（原 EA 也遵循这一限制）。当出现信号时，在当前买价/卖价基础上偏移 `EntryOffsetPoints` 点位布置止损挂单。
6. 挂单激活期间监控市场。如果挂单价格与当前买价/卖价之间的差异超过 `EntryOffsetPoints + SlippagePoints`，则取消原单并立刻按新的基准价重建，同时重新计算附带的止损和止盈价格。
7. 当实际点差超过 `MaxSpreadPoints` 时，为避免流动性不足的情况，会立即撤销所有待执行的入场挂单。

## 订单管理

* 挂单成交后记录成交价，并按照 `StopLossPoints` 与 `TakeProfitPoints` 的距离分别注册止损单和止盈单。若距离参数为零则跳过对应的保护单。
* 当 `UseTrailingStop` 为真时启动跟踪止损模块。程序跟踪最新买价/卖价，当价格朝持仓方向移动超过 `TrailingStopPoints` 时，通过 `ReRegisterOrder` 将止损单向有利方向移动。多头止损只会上移，空头止损只会下移。
* 持仓平仓后会撤销所有保护单并清空内部状态，持仓归零前不会评估新的入场信号。

## 参数

| 参数 | 说明 |
|------|------|
| `MaxSpreadPoints` | 允许的最大点差，超过后撤销挂单。 |
| `TakeProfitPoints` | 成交后止盈距离。 |
| `StopLossPoints` | 挂单与成交后使用的止损距离。 |
| `EntryOffsetPoints` | 相对买价/卖价布置止损挂单的偏移。 |
| `UseTrailingStop` | 是否启用跟踪止损。 |
| `TrailingStopPoints` | 跟踪止损与当前价格保持的距离。 |
| `FixedVolume` | 每次下单的固定交易量。 |
| `EnvelopePeriod` | LWMA 包络的计算周期。 |
| `EnvelopeDeviation` | 包络宽度（百分比）。 |
| `DistancePoints` | 触发信号所需的价格与包络最小距离。 |
| `SlippagePoints` | 重新定价阈值额外允许的点数。 |
| `CandleType` | 计算包络使用的时间框架（默认 M5）。 |

## 说明

* 策略同时订阅 K 线与 Level1 行情。若无法获得买价/卖价，则由于缺少点差与跟踪止损依据，信号不会触发。
* 每次跟踪止损调整价格时，会按照当前持仓量重新生成保护性止损与止盈单。
* 代码中的注释全部为英文，并统一使用制表符缩进以符合项目规范。
