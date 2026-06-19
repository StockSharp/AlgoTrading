# TDSGlobal 挂单策略（C#）

## 概述

本策略将 MetaTrader 5 专家顾问 **TDSGlobal**（源码位于 `MQL/23255/TDSGlobal.mq5`）移植到 StockSharp 高阶 API。策略在默认的四小时K线上计算 MACD 线、MACD 柱状图（OsMA）以及 Force Index。当这些指标组合给出潜在反转信号时，策略会在上一根 K 线的高低点附近挂出限价单，并通过止损、止盈与可选的追踪止损管理仓位。

## 交易逻辑

1. **指标计算**：使用 `MACD(12, 26, 9)` 取得 MACD 线和 OsMA，同时使用 `ForceIndex(24)` 评估前一根完成 K 线的力度。
2. **信号判定**：当有至少两根历史 MACD 与 OsMA 数据时，判断其斜率方向。OsMA 上升且前一根 Force Index 为负值时，准备挂出卖出限价单；OsMA 下降且前一根 Force Index 为正值时，准备挂出买入限价单。
3. **挂单价格**：卖出限价单略高于前一根 K 线最高价，买入限价单略低于最低价。如果距离当前买卖价不足，订单价格会按照 `EntryOffsetPips`（默认 16 点）进行调整。
4. **风险控制**：根据订单价格计算止损与止盈（若参数为零则禁用）。持仓后可按 `TrailingStopPips` 与 `TrailingStepPips` 更新追踪止损；若在一根 K 线内触发保护价，会立即使用市价单平仓。
5. **订单维护**：当 OsMA 斜率反向时，取消对应挂单；任一挂单成交后会撤销另一侧订单，避免同时暴露。

## 资金管理

- **固定手数**：`OrderVolume` 直接作为下单手数。
- **风险百分比**：启用 `UseRiskSizing` 时，根据组合权益和 `RiskPercent` 计算允许亏损金额，再除以止损距离得到下单手数，最后按照交易品种的最小手数对齐。

## 参数表

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 关闭风险管理时的固定手数。 | 1 |
| `UseRiskSizing` | 是否启用风险百分比资金管理。 | true |
| `RiskPercent` | 每笔交易愿意承担的权益百分比。 | 3 |
| `MacdFastPeriod` | MACD 快速 EMA 长度。 | 12 |
| `MacdSlowPeriod` | MACD 慢速 EMA 长度。 | 26 |
| `MacdSignalPeriod` | MACD 信号线 EMA 长度。 | 9 |
| `ForceLength` | Force Index EMA 平滑长度。 | 24 |
| `StopLossPips` | 止损距离（点）。0 表示不设置。 | 50 |
| `TakeProfitPips` | 止盈距离（点）。0 表示不设置。 | 50 |
| `TrailingStopPips` | 追踪止损距离（点）。 | 5 |
| `TrailingStepPips` | 追踪止损每次推进的最小步长（点）。 | 5 |
| `EntryOffsetPips` | 挂单相对前一根高低点的缓冲距离。 | 16 |
| `MinDistancePips` | 订单与保护价格的最小安全距离。 | 3 |
| `PipSize` | 单个点对应的价格增量。 | 0.0001 |
| `CandleType` | 使用的 K 线类型。 | 4 小时 |

## 使用步骤

1. 将 `CS/TdsGlobalPendingStrategy.cs` 添加到您的 StockSharp 项目或在回测器中动态加载。
2. 启动前指定交易品种与组合；若启用风险百分比，需要组合能够提供实时权益值。
3. 策略启动后需等待至少两根完成的 K 线以初始化指标斜率，此期间不会挂单。
4. 通过日志监控订单提交、撤销与追踪止损调整等事件，以便和原始 EA 行为对照。

## 与 MQL 版本的差异

- 在 StockSharp 中，挂单成交通过 `OnOwnTradeReceived` 异步处理，而不是同步返回结果。
- MetaTrader 的“冻结/止损距离”以 `MinDistancePips` 与基于点差的估算代替。
- 当 K 线内部触及保护水平时，通过市价单直接平仓，模拟原始 EA 的手动修改逻辑。

这些调整确保策略忠于原始思想，同时与 StockSharp 生态兼容。
