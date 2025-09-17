# LBS 策略

**LBS 策略** 是 MetaTrader 5 顾问 "LBS (barabashkakvn's edition)" 的 StockSharp 版本。原始程序在指定的交易时段内监测前一根 K 线的高低点，并在突破时放置止损买入/卖出单。本移植使用 StockSharp 的高级 API（`SubscribeCandles`、`SubscribeLevel1`、`BuyStop`/`SellStop` 等）实现同样的交易逻辑和资金管理规则。

## 交易流程

1. 策略订阅所选时间框 (`CandleType`) 的收盘蜡烛。
2. 当蜡烛的收盘时间等于任意启用的交易小时 (`Hour1`、`Hour2`、`Hour3`) 时，会计算突破价格：
   - Buy Stop 下单价取蜡烛最高价与当前卖价加“冻结”缓冲区中的较大值。
   - Sell Stop 下单价取蜡烛最低价与当前买价减“冻结”缓冲区中的较小值。
   - 缓冲区模拟 MetaTrader 的 `SYMBOL_TRADE_FREEZE_LEVEL` 行为：取三倍点差，但不低于十个点。
3. 当任一方向成交后，另一张挂单立即撤销，对应原版中的 `DeleteAllPendingOrders`。
4. `StopLossPips` 定义初始止损；若启用 `TrailingStopPips` 与 `TrailingStepPips`，当浮动盈利超过两者之和时，止损会沿趋势移动。
5. 仅在策略在线、没有持仓且有有效的 Level1 报价时才会发送订单。

## 资金管理

`MoneyMode` 参数对应原始顾问的“固定手数 / 风险百分比”开关：

- **FixedLot**：`VolumeOrRisk` 视为固定下单手数。
- **RiskPercent**：`VolumeOrRisk` 视为账户权益百分比。策略将风险金额除以入场价与止损价之间的距离（以价格步长计）来计算下单手数。使用该模式时必须启用止损，否则不会发送订单。

策略会按照交易品种的最小手数、步长和最大手数限制对计算结果进行归一化，避免被经纪商拒单。

## 参数列表

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `StopLossPips` | 50 | 固定止损距离（点）。设为 0 可关闭初始止损与追踪逻辑。 |
| `TrailingStopPips` | 5 | 追踪止损距离（点）。设为 0 可禁用追踪。 |
| `TrailingStepPips` | 15 | 移动止损前所需的额外盈利（点）。启用追踪时必须大于 0。 |
| `MoneyMode` | `FixedLot` | 资金管理模式：固定手数或风险百分比。 |
| `VolumeOrRisk` | 1.0 | 在 `FixedLot` 模式下为手数，在 `RiskPercent` 模式下为风险百分比。 |
| `Hour1` | 10 | 第一个交易小时，设为 `0` 表示关闭。 |
| `Hour2` | 11 | 第二个交易小时，设为 `0` 表示关闭。 |
| `Hour3` | 12 | 第三个交易小时，设为 `0` 表示关闭。 |
| `CandleType` | 1 小时 | 计算突破所用的蜡烛时间框。请与 MetaTrader 图表保持一致。 |

## 实现细节

- 通过蜡烛收盘时间来判断交易时段，对应 MetaTrader 中新柱形成时的 `TimeCurrent()`。
- 冻结/止损缓冲不低于十个点，可避免因经纪商限制导致的下单失败。
- 追踪止损在每个 Level1 价格变动时更新，以模拟原始 `OnTick` 回调的行为。
- 风险百分比模式优先使用 `Portfolio.CurrentValue`，若不可用则回退到 `Portfolio.BeginValue`。

## 使用建议

1. 选择目标品种和蜡烛时间框，使其与 MetaTrader 设置一致。
2. 设定需要交易的小时段，输入 `0` 即可关闭对应时间窗。
3. 若需按权益比例下单，将 `MoneyMode` 改为 `RiskPercent`，并确保 `StopLossPips` 为正数。
4. 若使用固定手数，保持 `MoneyMode = FixedLot`，并把 `VolumeOrRisk` 设置为期望手数。
5. 启动策略后，在下一次符合条件的交易小时会自动挂出双向突破单，并根据规则维护保护性止损。
