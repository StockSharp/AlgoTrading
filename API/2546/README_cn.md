# MacdPatternTrader 策略

## 概述
**MacdPatternTrader** 策略是 MQL5 智能交易系统 `MacdPatternTraderAll0.01` 的高阶 API 复刻版本。策略订阅所选 K 线序列，在每根收盘 K 线上同时评估六个独立的 MACD 入场模型。每个模型都拥有自己的快/慢 EMA 组合、止损回溯区间以及阈值，因此信号之间互不干扰。一旦模型触发，就会按照当前的马丁加仓量下达市价单，并立即计算保护价位。

该转换完整保留了原始 EA 的主要特性：

* 统一的马丁格尔资金管理模块，在亏损周期后将下次下单量翻倍，在盈利周期后恢复初始手数。
* 止损价取自最近 `StopLossBars` 根 K 线的最高价/最低价，再加上以点数表示的偏移量，完全复刻 MQL 中的 `iHighest`/`iLowest` 循环。
* 止盈价会按 `TakeProfitBars` 为一组逐段扫描，只要后续区段出现更远的极值就持续延伸目标位。
* 当浮动盈亏超过 5 个货币单位并且 EMA/SMA 滤波同意方向时，按“先 1/3、再剩余 1/2”的比例分批减仓，且最小减仓量不会低于 0.01。
* 可选的时间过滤器将开仓限制在 `(StartTime, StopTime)` 区间内，但风控仍会 24 小时运行。

## 数据与指标
* **数据类型**：可配置的 K 线类型（默认为 H1，与原始 EA 相同）。
* **指标**：六个 `MovingAverageConvergenceDivergenceSignal` 实例，以及四个用于分批止盈滤波的移动平均（EMA、EMA、SMA、EMA）。所有指标均通过 `BindEx` 与同一个订阅绑定，确保处理回调收到同步的指标值。
* **历史缓存**：维护一个精简的滚动 K 线窗口，用于止损/止盈扫描；同时缓存 MACD 值以复刻原始阶段计数逻辑。

## 入场逻辑
六个模型彼此独立，即使已有持仓仍可产生新信号，因此整体行为类似信号聚合器。

1. **模型 1 – 阈值反转**：MACD 主线突破 `Pattern1MaxThreshold` 后回落且仍处于正区间时做空；跌破 `Pattern1MinThreshold` 后回升则做多。
2. **模型 2 – 零轴回踩**：跟踪 MACD 是否保持在零轴之上，若重新跌破并形成向下钩形则准备做空；负区间恢复到零轴以上且连续两根柱体走高时做多。
3. **模型 3 – 分级峰/谷**：完全复刻原 EA 中的多阶段计数器（`S3`、`stops3`、`stops13` 等），以识别复杂的顶部和底部结构；每次交易后都会重置 `bars_bup` 计数。
4. **模型 4 – 局部极值**：当当前 MACD 回到阈值以内，且上一根柱体是相对于两根前的极值时，判定为局部峰值或谷值并执行反向交易。
5. **模型 5 – 中性带突破**：利用中性阈值定义一条缓冲带，价格先穿越中性带再突破趋势阈值时入场，做多与做空完全对称。
6. **模型 6 – 连续计数**：统计 MACD 何时连续保持在阈值之外，只有当计数落在 `Pattern6TriggerBars` 与上限/下限之间时才允许交易。

## 离场与风控
* **止损**：多单使用最近 `StopLossBars` 根 K 线的最低价加上偏移量；空单则取最高价。算法逐段扫描，直到找到符合条件的极值。
* **止盈**：按照 `TakeProfitBars` 为单位逐段寻找新的极值，只要下一段提供更有利的价位就延长目标，否则立即停止。
* **分批减仓**：浮盈超过 5 后首次减仓 1/3，第二次减掉剩余的一半；EMA/SMA 滤波用于确认趋势方向，且保证每次减仓量不少于 0.01。
* **马丁格尔**：`InitialVolume` 定义基础手数，若上一轮平仓亏损且 `UseMartingale` 启用，则把 `_currentVolume` 翻倍；盈利则复位。`_longPartialCount` 与 `_shortPartialCount` 记录已执行的分批次数，防止重复操作。
* **时间窗口**：启用 `UseTimeFilter` 后，仅当 K 线收盘时间严格介于 `StartTime` 与 `StopTime` 之间时才评估新信号；已有仓位的风控始终有效。

## 参数
| 分组 | 名称 | 说明 |
| --- | --- | --- |
| 模型 1 | `Pattern1Enabled` | 是否启用模型 1。 |
| 模型 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | 止损/止盈回溯区间及点差偏移。 |
| 模型 1 | `Pattern1Slow`, `Pattern1Fast` | MACD 的慢、快 EMA 周期。 |
| 模型 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | 触发多空的上下阈值。 |
| 模型 2 | 参数结构与模型 1 相同，数值独立。 |
| 模型 3 | 额外使用 `Pattern3MaxLowThreshold`、`Pattern3MinHighThreshold` 控制多阶段识别。 |
| 模型 4 | 保留 `Pattern4AdditionalBars`（兼容性字段），并提供额外的极值阈值。 |
| 模型 5 | 通过 `Pattern5MaxNeutralThreshold`、`Pattern5MinNeutralThreshold` 定义中性带范围。 |
| 模型 6 | `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` 控制连续计数的阈值。 |
| 管理 | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | 分批减仓使用的移动平均周期。 |
| 通用 | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | 全局行为和数据设置。 |

## 实现说明
* 完全依赖 StockSharp 高阶 API（`SubscribeCandles`、`BindEx`、`BuyMarket`、`SellMarket`），不直接拉取指标缓存。
* 通过 `SetStopLoss`/`SetTakeProfit` 在每根 K 线收盘后刷新保护位，使行为与原 EA 保持一致。
* 滚动集合限制为 1000 条记录，避免长时间运行时无限增长，同时保证逻辑一致性。
* `OnOwnTradeReceived` 中实时统计成交量与已实现盈亏，用于立即做出马丁格尔决策。
* 代码新增英文注释，方便维护与审核。

## 使用建议
1. 选择具备连续流动性的交易品种（外汇、差价合约或加密资产）。
2. 确认 K 线周期与原 EA 一致（默认 H1）。
3. 针对新市场时可调整各模型阈值或关闭部分模型以减少干扰。
4. 注意马丁格尔可能在连续亏损期间快速放大头寸，需结合账户风险承受能力使用。
