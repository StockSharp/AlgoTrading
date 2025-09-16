# Martingale Bone Crusher 策略

## 概述

**Martingale Bone Crusher 策略** 复刻了原始 MetaTrader 智能交易系统的核心逻辑。策略通过比较一快一慢两条简单移动平均线来确定做多或做空方向，并在出现亏损后采用马丁格尔资金管理模型放大下一笔订单的手数。策略同时提供了多种风险控制手段，包括固定金额止盈、百分比止盈、保本移动、以价格步长计量的传统止损/止盈以及以盈利金额为基础的移动止盈。

## 交易逻辑

- **信号生成**：在主图 K 线序列上计算两条简单移动平均线。当快线低于慢线时寻找做多信号；快线高于慢线时寻找做空信号；持仓未平仓前不会开出新的信号。
- **马丁格尔序列**：每当一笔交易结束，都会重新计算下一次下单手数。若上一笔交易亏损，则按照设置选择乘法放大或加法递增手数；若盈利，则手数恢复到初始值。
- **模式选择**：策略提供两个马丁格尔模式：
  - `Martingale1`：无论盈亏，下一笔始终跟随当前均线方向。
  - `Martingale2`：若上一笔亏损，则下一笔会反向开仓，复现原始 EA 的第二种逻辑。
- **风险管理**：持仓期间持续监控：
  - 以价格步长定义的固定止损与止盈；
  - 可选的价格追踪止损，使用固定步长追随极值；
  - 当价格向有利方向移动指定距离后自动将止损移至保本的功能；
  - 基于浮动盈亏的金额止盈与百分比止盈；
  - 当浮动盈利达到激活值后，按金额跟踪锁定收益的移动止盈。

## 参数

| 参数 | 说明 |
|------|------|
| `UseTakeProfitMoney` | 启用固定金额止盈。 |
| `TakeProfitMoney` | `UseTakeProfitMoney` 开启时触发平仓的金额。 |
| `UseTakeProfitPercent` | 启用以初始资金百分比衡量的止盈目标。 |
| `TakeProfitPercent` | `UseTakeProfitPercent` 开启时使用的百分比。 |
| `EnableTrailing` | 启用基于金额的移动止盈。 |
| `TrailingTakeProfitMoney` | 激活金额移动止盈所需的浮动收益。 |
| `TrailingStopMoney` | 金额移动止盈被激活后允许的利润回撤。 |
| `MartingaleMode` | 选择 `Martingale1` 或 `Martingale2` 的马丁格尔逻辑。 |
| `UseMoveToBreakeven` | 启用自动移至保本。 |
| `MoveToBreakevenTrigger` | 触发保本所需的价格步长数量。 |
| `BreakevenOffset` | 将止损移至保本时在入场价上添加的偏移。 |
| `Multiply` | `DoubleLotSize` 为 `true` 时，亏损后下一笔手数的倍增系数。 |
| `InitialVolume` | 首笔及获利后使用的基础下单手数。 |
| `DoubleLotSize` | 选择亏损后是使用乘法放大 (`true`) 还是加法递增 (`false`)。 |
| `LotSizeIncrement` | 当 `DoubleLotSize` 为 `false` 时，亏损后增加的手数。 |
| `TrailingStopSteps` | 价格追踪止损的步长。 |
| `StopLossSteps` | 传统止损的步长。 |
| `TakeProfitSteps` | 传统止盈的步长。 |
| `FastPeriod` | 快速简单移动平均的周期。 |
| `SlowPeriod` | 慢速简单移动平均的周期。 |
| `CandleType` | 指标计算所使用的 K 线类型。 |

## 说明

- 下单手数会按照交易品种的步长、最小手数与最大手数进行对齐。
- 浮动盈亏的金额计算依赖品种的 `PriceStep` 与 `StepPrice`。若二者为 0，则相关金额控制会自动跳过。
- 按要求仅提供 C# 版本，本次未创建 Python 实现。
