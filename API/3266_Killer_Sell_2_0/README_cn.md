# Killer Sell 2.0 策略（C#）

## 概览
Killer Sell 2.0 是一款仅做空的 MetaTrader 4 专家顾问，专注于在市场
极度超买时进场，并在动量转入超卖区间后保护利润。此版本基于
StockSharp 高层 API 重新实现，使用 `SubscribeCandles().BindEx(...)`
以事件驱动的方式更新指标，同时在策略内部封装资金管理规则。

## 交易逻辑
在所选周期的每根新收盘 K 线到来时，策略会按顺序执行以下步骤：

1. **数据准备。** 计算 MACD（12/120/9）、两条威廉指标（350 周期）
   以及两个随机指标（10/1/3 用于入场，90/7/1 用于离场）。只有当
   蜡烛状态为 `Finished` 时，这些指标值才会被使用。
2. **入场过滤。** 满足下列条件才会开启新的空头：
   - 威廉指标上穿 −10，表明市场已进入超买区。
   - MACD 主线高于 `0.0014`。
   - 入场随机指标的 %K 由上向下穿越指定阈值（默认 90）。
3. **下单执行。** 当所有过滤条件同时满足时，策略按照当前的
   马丁格尔仓位大小发送市价卖单，并通过 `StartProtection` 附加
   100 点（默认值，可调）的保护性止盈。
4. **离场管理。** 只要存在空头持仓，就会计算所有未平仓单的平均
   盈利（以点数表示）：
   - 若平均收益低于 10 点且威廉指标跌破 −80，则立即平掉全部空单。
   - 若平均收益高于 15 点且离场随机指标 %K 低于 12，则锁定利润退出。

## 资金管理
原始 EA 使用的“马丁格尔”分批方式在 C# 版本中得以保留。策略维护
一个内部列表记录每笔做空的价格与手数，从而复现 MetaTrader 中的
逐单计算逻辑：

- 第一笔仓位使用 `InitialVolume`（默认 0.05 手）。
- 当一轮交易盈利或打平时，下一笔仓位重置为初始手数。
- 当一轮交易亏损时，下一笔仓位按 `MartingaleMultiplier`
  （默认 ×1.2）放大，`MaxVolume` 用于限制最大手数。

同时，策略会在成交时追踪已实现盈亏，以判断上一轮的结果。

## 参数说明
| 参数 | 含义 |
|------|------|
| `CandleType` | 指标计算所使用的主周期。 |
| `EntryWprPeriod` / `ExitWprPeriod` | 入场/离场威廉指标的周期。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD 的三个周期参数。 |
| `MacdThreshold` | 触发做空所需的最小 MACD 主线值。 |
| `StochasticEntryKPeriod`、`StochasticEntryDPeriod`、`StochasticEntrySlow` | 入场随机指标的参数。 |
| `EntryStochasticLevel` | %K 需要向下穿越的阈值。 |
| `StochasticExitKPeriod`、`StochasticExitDPeriod`、`StochasticExitSlow` | 离场随机指标的参数。 |
| `ExitStochasticLevel` | 离场时判定超卖的上界。 |
| `EntryWprThreshold` / `ExitWprThreshold` | 入场/离场威廉指标的阈值。 |
| `LossExitPips` / `ProfitExitPips` | 触发防守/获利离场的平均点数阈值。 |
| `TakeProfitPips` | 每笔空单附加的保护性止盈距离。 |
| `InitialVolume` | 马丁格尔的起始手数。 |
| `MartingaleMultiplier` | 亏损后使用的放大系数。 |
| `MaxVolume` | 单笔交易允许的最大手数。 |

## 转换注意事项
- MetaTrader 采用逐单持仓模型，而 StockSharp 使用净头寸。为了复现
  平均盈利和马丁格尔重置逻辑，策略会记录每笔卖出成交的价格与数量。
- 原始项目虽然包含多种资金管理模式，但实际配置只启用了马丁格尔。
  因此 C# 版本仅实现该分支。
- 源代码关闭了硬性止损。本移植版也仅设置保护性止盈，其余退出逻辑
  完全由指标条件驱动。

## 使用建议
1. 将策略绑定到相应的投资组合与交易品种，并设置与 MT4 测试相同的
   时间周期（默认假设使用 H1）。
2. 确保行情源能够提供完整的收盘 K 线，否则指标将无法成型。
3. 根据账户杠杆和经纪商要求调整 `InitialVolume`、`MaxVolume` 等参数。
4. 马丁格尔策略在趋势行情中风险较大，请务必先行回测并严格控制仓位。

