# Constituents EA 策略
[English](README.md) | [Русский](README_ru.md)

该策略把 `MQL/22595` 中的 **Constituents EA** 移植到 StockSharp 的高级 API。它会在指定的小时附近，围绕最近的价
格区间自动挂出两张挂单，并利用 StockSharp 自带的风控工具来处理止损和止盈。

## 策略流程

1. **时间过滤**：每根 K 线收盘时，策略检查下一根 K 线是否会在 `StartHour` 指定的小时开始。只有满足该条件时才会再次生成新的挂单，这一点与原始 MT5 程序完全一致。
2. **区间计算**：使用 `Highest` 与 `Lowest` 指标跟踪最近 `SearchDepth` 根已完成 K 线的最高点与最低点，作为挂单的价
格水平。
3. **距离限制**：策略通过订阅盘口，实时获得最优买价/卖价。只有当挂单价格与当前报价之间的距离大于或等于
`MinOrderDistancePips`（按 `PointValue` 转换为绝对价格）时才会提交订单，从而复现原代码中的冻结区间检查。
4. **挂单类型**：`PendingOrderMode` 用于选择挂单类型。`Limit` 表示在区间内做回归（低位 buy limit、高位 sell limit），
`Stop` 表示做突破（高位 buy stop、低位 sell stop）。两张挂单会同时提交。
5. **风险控制**：`StartProtection` 会根据 `StopLossPips` 与 `TakeProfitPips` 自动附加止损和止盈。参数
`MinStopDistancePips` 用来模拟 MT5 中的 `StopsLevel` 校验，避免止损/止盈距离过近。
6. **订单管理**：一旦其中一张挂单成交，另一张会立即撤单。在存在活跃挂单期间不会重复下单，这与 MetaTrader 的实
现一致。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `StartHour` | 生成挂单的小时（0-23）。 |
| `SearchDepth` | 计算最高价/最低价时所使用的已完成 K 线数量。 |
| `PendingOrderMode` | 选择挂单类型：`Limit` 为回归挂单，`Stop` 为突破挂单。 |
| `StopLossPips` | 止损距离（单位：点），0 表示不使用止损。 |
| `TakeProfitPips` | 止盈距离（单位：点），0 表示不使用止盈。 |
| `PointValue` | 每一个点对应的价格增量。设为 0 时将尝试从 `PriceStep`/`MinStep` 自动推断。 |
| `MinOrderDistancePips` | 当前报价与挂单价格之间允许的最小距离，用于模拟交易商的冻结区间。 |
| `MinStopDistancePips` | 止损/止盈必须满足的最小距离，用于模拟 `StopsLevel` 检查。 |
| `CandleType` | 进行计算与调度时所使用的 K 线类型。 |

下单数量由 `Strategy.Volume` 控制，请确保该值为正，以便 `BuyLimit`、`SellLimit`、`BuyStop`、`SellStop` 能够提交订单。

## 使用步骤

1. 将策略绑定到目标品种，并设置 `CandleType`（时间框架）。
2. 根据原 MT5 参数调整 `StartHour` 与 `SearchDepth`，必要时修改 `Min*Pips` 以满足经纪商的最小距离要求。
3. 如果自动推断 `PointValue` 失败（例如交易合成品或差价合约），请手工设置点值。
4. 设定 `StopLossPips`、`TakeProfitPips`、`MinOrderDistancePips`、`MinStopDistancePips` 以符合交易规则。
5. 设置 `Volume` 后启动策略。系统会自动订阅 K 线与盘口，在指定时刻挂出两张订单，并在成交后撤销另一张挂单。

## 与原版 EA 的差异

- 原始 EA 的 `MoneyFixedMargin` 风控（按账户百分比计算手数）未实现。请直接设置 `Volume`，或在外部封装自己的风
  控模块。
- 冻结区间与最小止损距离通过参数 `MinOrderDistancePips`、`MinStopDistancePips` 控制，因为部分经纪商不会在数据中
  提供这些限制。
- 策略在上一根 K 线收盘、并且下一根 K 线开盘时间等于 `StartHour` 时提交挂单，这与 MetaTrader 的执行时机相同。
- 代码注释统一改为英文，文档提供英文、俄文和中文三个版本，便于跨语言使用。

在点差较大的市场中，通常需要增加 `MinOrderDistancePips` 或者扩大止损/止盈距离，才能避免挂单被经纪商拒绝。
