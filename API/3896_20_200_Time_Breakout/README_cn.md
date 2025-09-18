# Twenty200 Time Breakout

本策略移植自 MetaTrader 专家顾问 **20/200 expert v4.2 (AntS)**。它每天在指定的交易小时对比两根历史小时K线的开盘价（默认分别是当前K线向前 6 根与 2 根）。若远端开盘价高于近端开盘价超过 `Short Delta` 点，则开空；反之若近端开盘价高出远端超过 `Long Delta` 点，则开多。

## 交易逻辑

- 订阅的K线类型默认为 1 小时，可通过 `Candle Type` 调整。
- 每个交易日只允许一笔仓位；只有当当前K线的小时数等于 `Trade Hour` 时才会评估信号。
- 信号使用 `LookbackFar` 与 `LookbackNear` 指定的历史开盘价：
  - **做空：** `Open[t1] - Open[t2] > Short Delta × 点值`。
  - **做多：** `Open[t2] - Open[t1] > Long Delta × 点值`。
- 触发信号后按计算的手数发送市价单。止损/止盈与原EA一致，以点数输入并通过 `Security.PriceStep` 自动换算为价格。
- 同一时间只保留一个方向的仓位，下一日可以重新建仓。

## 仓位管理

- 每次收到K线更新都会用最高价/最低价判断止盈或止损是否被触发。
- `Max Open Hours` 指定仓位的最长持有时间（默认 504 小时），超过后立即市价平仓，填写 0 可关闭该保护。

## 资金管理

- `Fixed Volume` 是关闭自动手数或无法获得账户权益时使用的基础手数。
- 启用 `Use Auto Lot` 时，手数遵循原EA中庞大的分段表。在 StockSharp 里通过 `volume = round(balance × Auto Lot Factor, 2)`（默认系数 `0.000038`）进行拟合，在 300~270000+ 美元区间内与原表精度保持在 0.01 手以内。
- 当当前权益低于上一次记录的余额时，下一笔交易会乘以 `Big Lot Multiplier`，复现 EA 中的 “Big Lot” 回本模式。
- 手数会根据 `Security.VolumeStep` 做步长对齐，并限制在交易所提供的 `MinVolume`/`MaxVolume` 区间内。

## 与 MT4 版本的差异

- MT4 程序显式写入了上千行阈值。本移植版使用线性系数 `Auto Lot Factor` 拟合同样的阶梯曲线，如需与其他经纪商完全一致可自行调整该系数。
- 止损/止盈通过在K线触及水平时发送市价平仓实现，无需依赖交易所的止损单支持，在回测与实盘中表现一致。
- 原策略依赖的全局变量（`globalBalans`、`globalPosic`）被内存状态替代，不再需要终端全局存储。

## 参数

| 参数 | 说明 |
|------|------|
| Long/Short Take Profit | 止盈距离（点）。 |
| Long/Short Stop Loss | 止损距离（点）。 |
| Trade Hour | 允许开仓的小时（0–23）。 |
| Far/Near Lookback | 两个历史开盘价的回溯数量。 |
| Long/Short Delta | 触发信号所需的点差。 |
| Max Open Hours | 仓位最长持有时间（小时，0 表示禁用）。 |
| Fixed Volume | 关闭自动手数时的基础仓位。 |
| Use Auto Lot | 是否根据账户权益自动计算手数。 |
| Auto Lot Factor | 拟合 MT4 手数阶梯的乘数系数。 |
| Big Lot Multiplier | 权益回撤后下一笔交易的放大系数。 |
| Candle Type | 信号使用的K线周期。 |
