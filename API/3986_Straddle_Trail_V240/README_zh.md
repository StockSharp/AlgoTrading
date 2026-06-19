# Straddle Trail v2.40 策略

**Straddle Trail v2.40** 策略复刻了 MetaTrader 4 智能交易系统 “Straddle&Trail” (v2.40)。该策略会在重大事件前布置一组对称的止损单，并在订单触发后执行保本与跟踪止损，同时也会管理已经存在的手动仓位。

## 工作流程

1. **准备阶段**
   - 订阅盘口和分钟级别（可配置）的K线，以获取最新买卖价并驱动时间逻辑。
   - 根据交易品种的小数精度计算每个“点”的大小，使所有以点为单位的参数能够准确转换为价格。
2. **挂出双向止损**
   - 在事件开始前 `PreEventEntryMinutes` 分钟（或在启用 `PlaceStraddleImmediately` 时立即）同时挂出买入止损和卖出止损，距离为 `DistanceFromPrice` 点。
   - 若启用 `AdjustPendingOrders`，策略会在事件前每分钟重新定位挂单；当距离事件不足 `StopAdjustMinutes` 分钟时停止调整。
3. **订单控制**
   - 当一侧成交后，可选的 `RemoveOppositeOrder` 会移除相反方向的挂单，避免双向持仓。
   - `ShutdownNow` 配合 `ShutdownOption` 可在需要时平掉持仓或撤销挂单。
4. **头寸保护**
   - 初始的止损和止盈均基于点数参数计算。
   - 达到 `BreakevenTriggerPips` 后，止损会移动到保本价并锁定 `BreakevenLockPips` 点的利润。
   - 根据 `TrailAfterBreakeven` 控制，跟踪止损可以立即启动或等待保本完成。
   - 当 `ManageManualTrades` 为真时，策略也会按同样的规则处理手动仓位。

## 参数说明

| 参数 | 说明 |
|------|------|
| `ShutdownNow` | 在下一根K线完成时执行紧急关闭逻辑。 |
| `ShutdownOption` | 控制关闭的范围：全部、仅已触发仓位、仅多头、仅空头、所有挂单、仅买入止损或仅卖出止损。 |
| `DistanceFromPrice` | 挂单距离当前价格的点数。 |
| `StopLossPips` | 初始止损距离（点）。 |
| `TakeProfitPips` | 初始止盈距离（点），为0时禁用。 |
| `TrailPips` | 跟踪止损距离（点），为0时禁用。 |
| `TrailAfterBreakeven` | 若为真，跟踪止损只会在达到保本后启动。 |
| `BreakevenLockPips` | 保本后锁定的利润（点）。 |
| `BreakevenTriggerPips` | 触发保本所需的浮盈（点）。 |
| `EventHour` / `EventMinute` | 事件的经纪商时间。将二者设为0可关闭事件调度，仅保留手动模式。 |
| `PreEventEntryMinutes` | 在事件前多少分钟布置双向止损。 |
| `StopAdjustMinutes` | 距离事件多少分钟后停止调整挂单，最小值为1分钟。 |
| `RemoveOppositeOrder` | 成交后是否删除相反方向的挂单。 |
| `AdjustPendingOrders` | 是否在事件前每分钟重新定位挂单。 |
| `PlaceStraddleImmediately` | 是否在策略启动时立即布置双向止损，忽略事件时间。 |
| `ManageManualTrades` | 是否对手动仓位应用保本与跟踪止损逻辑。 |
| `CandleType` | 用于驱动逻辑的K线类型（默认1分钟）。 |

## 使用建议

- 请确保交易品种的点值设置正确，以便点数距离能准确映射到价格。
- 策略在满足止损或止盈条件时通过市价单平仓，与原版 EA 手动调整止损的方式一致。
- 当启用事件调度且未开启 `PlaceStraddleImmediately` 时，每个交易日仅布置一次双向止损；如需在同一天再次执行，请重新启动策略。
- 紧急关闭功能可以在市场波动加剧时快速清理风险敞口。

## 转换说明

- 代码中的注释全部为英文，并补充了额外说明，便于理解实现细节。
- 采用 StockSharp 的高层API（例如 `BuyStop`、`SellStop`、`ClosePosition`），符合项目的最佳实践要求。
- 根据 `AGENTS.md` 的规则，策略依靠订阅的数据流而非直接读取指标值。
