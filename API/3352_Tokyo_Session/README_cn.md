# Tokyo Session 策略

## 概述

Tokyo Session 策略将 MetaTrader 专家顾问 *TokyoSessionEA_v2.8* 的交易思想移植到 StockSharp 平台。策略围绕亚
洲（东京）交易时段工作，在设定的小时捕捉一根参考 K 线，建立价格区间，并在另一预设小时检查是否发生
突破或回撤。依据 `TypeOfSignals` 参数，可选择反向交易模式（`ContraryTrend`，反向做单以博取回归）或趋势
跟随模式（`AccordingTrend`，顺势跟随突破）。

移植版本完全基于 StockSharp 的高层 API 实现。信号逻辑在蜡烛订阅回调中完成，止盈止损/追踪止损由
`StartProtection` 管理，所有关键事件通过 `LogInfo` 记录，便于在回测和实时运行中追踪行为。

## 交易逻辑

1. **参考蜡烛**：在 `TimeSetLevels`（经偏移后的经纪商小时）记录蜡烛的最高价、最低价和收盘价，形成当日
   的价格通道，同时重置内部校验标记。
2. **通道校验**：在参考小时与进场小时之间的每根收盘蜡烛都会触发校验：
   - `CheckAllBars` 启用时，所有收盘价必须始终位于通道内。
   - `ReCheckPrices` 在 `TimeRecheckPrices` 时刻比较收盘价与滚动平均值，确认动量方向。
3. **进场判断**：当紧邻 `TimeCheckLevels` 之前的蜡烛收盘时，将其收盘价与通道边界进行比较，若满足
   `[MinDistanceOfLevel, MaxDistanceOfLevel]` 区间限制，则按模式开多或开空。
4. **离场机制**：
   - `CloseInSignal`：价格重新回到通道内时立即平仓。
   - `CloseOrdersOnTime`：在 `TimeCloseOrders` 指定小时后强制平仓，避免隔夜持仓。
   - `StartProtection`：根据参数自动下达止盈、止损、追踪止损与保本移动。

## 参数说明

### 通用

| 参数 | 说明 |
|------|------|
| `CandleType` | 用于分析的 K 线类型（默认 H1）。 |
| `BrokerOffset` | 经纪商时间相对于 GMT 的偏移（小时）。 |

### 信号设置

| 参数 | 说明 |
|------|------|
| `TypeOfSignals` | `ContraryTrend` 反向交易；`AccordingTrend` 顺势交易。 |
| `TimeSetLevels` | 捕捉参考蜡烛的小时。 |
| `TimeCheckLevels` | 执行进场判定的小时。 |
| `TimeRecheckPrices` | 额外动量确认的小时。 |
| `MinDistanceOfLevel` | 收盘价与通道边界的最小距离（点）。 |
| `MaxDistanceOfLevel` | 收盘价与通道边界的最大距离（点，0 表示不限制）。 |
| `ReCheckPrices` | 是否启用额外动量校验。 |
| `CheckAllBars` | 是否要求所有中间蜡烛收盘价保持在通道内。 |

### 风险控制

| 参数 | 说明 |
|------|------|
| `CloseInSignal` | 当出现反向信号时立即平仓。 |
| `CloseOrdersOnTime` | 到达设定时间后平仓。 |
| `TimeCloseOrders` | 强制平仓的小时。 |
| `UseTakeProfit`, `TakeProfit` | 固定止盈距离（点）。 |
| `UseStopLoss`, `StopLoss` | 固定止损距离（点）。 |
| `UseTrailingStop`, `TrailingStop`, `TrailingStep` | StockSharp 追踪止损设置（点）。 |
| `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | 当盈利达到阈值时将止损移动到保本。 |

### 交易参数

| 参数 | 说明 |
|------|------|
| `Volume` | 基础下单数量；当反向开仓时会先平掉原有仓位。 |
| `MaxOrders` | 同方向最多允许的 `Volume` 倍数，0 表示不限制。 |

## 使用步骤

1. 选择带有有效价格步长 (`Security.PriceStep`) 的交易品种。
2. 设置合适的蜡烛周期与经纪商时区偏移，使策略时间点与市场节奏匹配。
3. 根据市场特性或原始 EA 预设微调距离与过滤条件。
4. 配置风险管理参数；止盈止损逻辑由 `StartProtection` 自动执行。
5. 启动策略，并通过日志信息监控水平捕捉、进出场决策与风控动作。

## 与 MetaTrader 版本的差异

- 未实现浮动挂单逻辑（`UseFloatingPoint`, `PipsFloatingPoint`），StockSharp 版本在信号触发时直接发送市价单。
- 由于高层蜡烛订阅无法获得即时 Bid/Ask 数据，移植版本省略了原策略中的点差及滑点过滤器。
- 自动资金管理（`AutoLotSize`, `RiskFactor`, 亏损加仓恢复机制、预设品种切换）被简化为 `Volume` 与
  `MaxOrders` 两个参数，需要自行在配置中设置仓位规模。
- 声音提醒与日志输出改由 `LogInfo` 完成，方便统一管理。

除上述差异外，其余交易规则与原始 EA 保持一致。

## 注意事项

- 默认配置针对 H1 周期设计。若使用其他周期，应确保其时长能被 1 小时整除，以便小时级逻辑正常运作。
- 缺失蜡烛或数据断档会影响平均值和通道校验，可能导致信号被取消。
- 策略通过市价单平仓，如经纪商要求限价单或限制持仓时间，需要额外扩展策略逻辑。
