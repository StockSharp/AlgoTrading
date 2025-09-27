# FiftyFiveMaBarComparisonStrategy

## 概述
该策略复刻 MetaTrader 5 的“55 MA”专家顾问，通过比较 55 周期移动平均线在两个柱上的数值，只要差值超过可配置阈值就执行交易。所有计算都基于已收盘的K线，并限制在自定义的日内时间窗口内进行，同时可以选择反转信号方向。算法保持原版EA的行为——如果没有满足做多条件，会默认开空单。

## 交易逻辑
1. 订阅指定的K线类型，并根据所选周期、方法和价格类型计算移动平均线。
2. 维护一个最近的均线数值缓冲区，即便设置了水平位移也能访问到 `BarA` 和 `BarB` 的均线值。
3. 当收到位于 `[StartHour, EndHour)` 时段内的收盘K线时：
   - 读取 `BarA + MaShift` 与 `BarB + MaShift` 处的均线值。
   - 如果 `BarA` 处的均线值大于 `BarB` 处的均线值加上 `DifferenceThreshold`，则在未启用 `ReverseSignals` 时买入，启用时卖出。
   - 如果 `BarA` 处的均线值小于 `BarB` 处的均线值减去 `DifferenceThreshold`，则在未启用 `ReverseSignals` 时卖出，启用时买入。
   - 若差值未达到阈值，策略沿用原EA的默认行为并触发卖出。
4. 始终使用策略的 `Volume` 以市价下单；当 `CloseOppositePositions` 启用时，会增加下单数量以先平掉反向持仓。
5. 可选的止损和止盈通过 `StartProtection` 添加，距离以点（pip）表示。对于报价小数位为3或5位的品种，1 pip 等于 `PriceStep` 乘以 10。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1分钟周期 | 用于计算和发出信号的K线序列。 |
| `StopLossPips` | `int` | 30 | 止损距离（单位：pip）。设为 0 表示禁用。 |
| `TakeProfitPips` | `int` | 50 | 止盈距离（单位：pip）。设为 0 表示禁用。 |
| `StartHour` | `int` | 8 | 交易窗口的起始小时（含）。 |
| `EndHour` | `int` | 21 | 交易窗口的结束小时（不含），必须大于 `StartHour`。 |
| `DifferenceThreshold` | `decimal` | 0.0001 | 触发方向信号所需的最小均线差值。 |
| `BarA` | `int` | 0 | 均线比较的第一个柱索引（0 表示当前K线）。 |
| `BarB` | `int` | 1 | 均线比较的第二个柱索引。 |
| `ReverseSignals` | `bool` | `false` | 反转做多/做空条件。 |
| `CloseOppositePositions` | `bool` | `false` | 若启用，在开新单前先平掉反向持仓。 |
| `MaShift` | `int` | 0 | 均线的水平位移，正值访问更早的均线点。 |
| `MaLength` | `int` | 55 | 移动平均线周期。 |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | 均线类型（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。 |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | 均线所使用的价格（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 |

## 仓位管理
- 通过设置策略 `Volume` 控制基础下单数量；启用 `CloseOppositePositions` 时会自动叠加当前反向仓位的绝对值。
- 仅当止损或止盈的pip距离大于0时才会调用 `StartProtection` 添加保护。

## 备注
- 交易时间窗口基于标的物的时间，处于 `[StartHour, EndHour)` 之外的信号会被忽略。
- 当 `MaShift` 产生负索引时，策略会等待更多历史数据，与原EA在位移导致 `EMPTY_VALUE` 时的处理方式一致。
- 原始EA在差值未达阈值时默认卖出，本策略保留该特性；若不希望如此，可调大 `DifferenceThreshold`。 
