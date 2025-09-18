# KA-Gold Bot 策略

**KA-Gold Bot 策略** 是 MetaTrader 4「KA-Gold Bot」EA 的 StockSharp 高阶版移植。策略结合 Keltner 通道、双 EMA 趋势过滤与多阶段风险管理：固定止损/止盈、分段追踪止损，以及基于实时报价的点差过滤。只有在指定的交易时段内且实时点差满足要求时，才会开仓。

## 交易逻辑

1. **指标准备**
   - `KeltnerPeriod` 长度的指数移动平均 (EMA) 作为通道中轴。
   - 同周期的简单移动平均应用于 K 线振幅（高点减低点），得到通道半宽度。
   - `EmaShortPeriod` 与 `EmaLongPeriod` 两条 EMA 分别捕捉短线动量与大周期趋势。
   - 所有指标值都会保存最近两个已完成 K 线的数据，以复刻 MT4 中的 `shift` 引用方式。

2. **入场条件**
   - 仅在当前 K 线收盘、策略已连接且允许交易时评估信号。
   - 最近两根 K 线的上轨/下轨通过中轴 ± 平均振幅计算得到。
   - **做多条件：**
     - 前一根收盘价突破最新上轨；
     - 同一收盘价高于慢速 EMA，确认上升趋势；
     - 快速 EMA 从上一根的上轨下方穿越到最新上轨上方（`EMA_short[2] < Upper[2]` 且 `EMA_short[1] > Upper[1]`）。
   - **做空条件：**
     - 前一根收盘价跌破最新下轨；
     - 同一收盘价低于慢速 EMA，确认下降趋势；
     - 快速 EMA 从上一根的下轨上方穿越到最新下轨下方（`EMA_short[2] > Lower[2]` 且 `EMA_short[1] < Lower[1]`）。
   - 仅允许单笔持仓，如已有仓位则忽略新信号。

3. **时间与点差过滤**
   - 当 `UseTimeFilter` 为 `true` 时，新仓位只会在 `[StartHour:StartMinute, EndHour:EndMinute)` 的交易时段内触发，使用交易所本地时间。若结束时间早于开始时间，则视为跨日时段。
   - 策略订阅 Level-1 行情，实时记录买一/卖一。开仓前会将当前点差换算为价格步长 (`PriceStep`) 的倍数，并与 `MaxSpreadPoints` 比较，超过阈值则跳过交易并记录日志。

4. **仓位管理**
   - 默认使用 `FixedVolume` 固定手数。若 `UseRiskPercent` 为 `true`，则按账户权益的 `RiskPercent%` 计算风险金额，并除以 `riskPips * PipValue` 得到下单量，其中 `riskPips` 优先采用 `StopLossPips`（如未设置，则退回 `TrailingStopPips`）。计算结果会按交易所的最小变动手数归一，并限制在允许的最小/最大量之间。
   - 做多时设置：
     - 固定止损 `entry - StopLossPips * pipSize`（若启用）；
     - 固定止盈 `entry + TakeProfitPips * pipSize`（若启用）；
     - 重置追踪止损状态，并清空空头保护变量。
   - 做空逻辑完全对称。

5. **追踪止损**
   - 实时报价驱动多空两套追踪机制：
     - 浮动盈利达到 `TrailingTriggerPips` 后激活追踪；
     - 追踪止损始终保持在当前有利价格之外 `TrailingStopPips` 个点，且只有在盈利超出上一止损位 `TrailingStopPips + TrailingStepPips` 时才会上移/下移；
     - 做多时，追踪止损不会低于初始止损；做空时则不会高于初始止损。
   - 退出监控同样在报价与收盘 K 线上执行：
     - 价格触及任意有效止损（初始或追踪）立即平仓；
     - 若最高/最低价触及止盈水平，也会立即了结仓位。
   - 平仓后所有保护状态清零，避免遗留数据影响下一笔交易。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 执行所用的 K 线数据类型。 | 1 分钟 K 线 |
| `KeltnerPeriod` | Keltner 中轴及振幅均值的周期。 | 50 |
| `EmaShortPeriod` | 快速 EMA 周期。 | 10 |
| `EmaLongPeriod` | 慢速 EMA 周期，用作趋势过滤。 | 200 |
| `FixedVolume` | 固定下单量（关闭风险百分比时使用）。 | 1 |
| `UseRiskPercent` | 是否启用按百分比计算持仓。 | `true` |
| `RiskPercent` | 单笔交易占账户权益的百分比。 | 1 |
| `StopLossPips` | 固定止损的点数距离（0 表示禁用）。 | 500 |
| `TakeProfitPips` | 固定止盈的点数距离（0 表示禁用）。 | 500 |
| `TrailingTriggerPips` | 激活追踪止损所需的盈利点数。 | 300 |
| `TrailingStopPips` | 追踪止损与价格之间的距离。 | 300 |
| `TrailingStepPips` | 追踪止损每次移动所需的最小新增盈利。 | 100 |
| `UseTimeFilter` | 是否启用交易时段过滤。 | `true` |
| `StartHour` / `StartMinute` | 交易时段起始时间（交易所本地）。 | 02:30 |
| `EndHour` / `EndMinute` | 交易时段结束时间（交易所本地）。 | 21:00 |
| `MaxSpreadPoints` | 允许的最大点差（以价格步长计，0 表示不限制）。 | 65 |
| `PipValue` | 每个点 (pip) 的货币价值，用于风险持仓计算。 | 1 |

## 其他说明

- Pip 换算遵循交易所小数位规则：若品种为 5 位报价（小数位为奇数），则将价格步长乘以 10，与 MT4 中的 pip 定义保持一致。
- 策略同时订阅 K 线与 Level-1 数据，但不会额外向 `Strategy.Indicators` 注册指标，符合高阶 API 要求。
- 止损/止盈由策略以市价单执行，未向交易所提交真实的止损/止盈委托。
- 根据需求，本次未提供 Python 版本或对应文件夹。
