# Martingail Expert 策略

## 概述
- 将 MetaTrader 5 的 **MartingailExpert.mq5** 顾问迁移到 StockSharp。
- 使用可配置的随机指标 %K、%D 及平滑参数产生交易信号。
- 组合了顺势加仓与逆势加仓的马丁格尔网格结构，逐步放大仓位规模。
- 采用净持仓模型，只维护一个多头或空头的合并仓位。

## 交易逻辑
### 入场条件
1. 策略处理 `CandleType` 指定周期的已完成 K 线。
2. 随机指标取上一根完结 K 线的值，对应 MQL 中 `iStochastic(..., 1)` 的行为。
3. 满足以下条件时开多：
   - 上一个 %K 高于上一个 %D；
  - 上一个 %D 高于 `BuyLevel`；
   - 当前没有持仓。
4. 满足以下条件时开空：
   - 上一个 %K 低于上一个 %D；
   - 上一个 %D 低于 `SellLevel`；
   - 当前没有持仓。
5. 所有市价单都会将 `Volume` 归一化到最近的 `Security.VolumeStep` 步长。

### 仓位扩展
- `ProfitPips` 定义了顺势加仓的距离（以点为单位）。
  - 多头：当 K 线最高价触及 `lastEntryPrice + ProfitPips * positionCount` 时，再下单一个基础手数。
  - 空头：当 K 线最低价触及 `lastEntryPrice - ProfitPips * positionCount` 时，再下单一个基础手数。
- `StepPips` 定义了逆势加仓（马丁格尔）的距离。
  - 多头：当最低价跌至 `lastEntryPrice - StepPips` 时，下一单的数量为 `lastVolume * Multiplier`。
  - 空头：当最高价涨至 `lastEntryPrice + StepPips` 时，下一单的数量同样为 `lastVolume * Multiplier`。
- 每笔成交都会刷新 `lastEntryPrice`、`lastVolume` 以及当前持仓计数。

### 平仓逻辑
- 保存最近一次成交的价格，用于判断平仓。
- 当价格达到 `lastEntryPrice ± ProfitPips`（多头使用最高价，空头使用最低价）时，使用市价单全部离场。
- 持仓归零后，所有马丁格尔状态变量立即重置。

## 参数说明
| 参数 | 默认值 | 描述 |
| --- | --- | --- |
| `Volume` | `0.03` | 初始下单及顺势加仓的基础手数。 |
| `Multiplier` | `1.6` | 逆势加仓时的马丁格尔倍数。 |
| `StepPips` | `25` | 触发逆势加仓的点数距离。 |
| `ProfitPips` | `9` | 用于止盈及顺势加仓的点数距离。 |
| `KPeriod` | `5` | 随机指标 %K 的计算周期。 |
| `DPeriod` | `3` | 随机指标 %D 的平滑周期。 |
| `Slowing` | `3` | 应用于 %K 的额外平滑长度。 |
| `BuyLevel` | `20` | 允许做多的最小 %D 值。 |
| `SellLevel` | `55` | 允许做空的最大 %D 值。 |
| `CandleType` | 5 分钟 | 构建 K 线和指标的时间框架。 |

## 实现细节
- 点值通过 `Security.PriceStep` 计算，若价格有 3 或 5 位小数，则自动乘以 10 来复现 MQL 的点差算法。
- 订单数量按照 `Security.VolumeStep` 向下取整，若结果小于允许的最小步长则不会下单。
- 由于使用高层 API，策略通过 K 线的最高价/最低价来近似原策略在每个 tick 上的触发行为。
- `OnOwnTradeReceived` 事件用于记录真实成交价格和数量，保持马丁格尔序列的准确性。

## 使用建议
- 将 `CandleType` 调整为与原始 MT5 模板相同的周期（通常为 M5），以获得接近的表现。
- 确保品种的价格步长和手数步长在证券信息中正确配置，否则需要手动校准 `Volume`、`StepPips` 与 `ProfitPips`。
- 马丁格尔策略会在不利行情中增加风险，建议结合外部风控（止损或资金限制）。

## 与原版的差异
- StockSharp 版本基于已完成的 K 线进行计算，并利用最高/最低价近似原有的逐 Tick 判断。
- MetaTrader 中的可用保证金检查在此实现中缺失，需要通过外部手段控制风险。
- 策略运行在净持仓模式，不支持原版可能使用的对冲账户模式。
