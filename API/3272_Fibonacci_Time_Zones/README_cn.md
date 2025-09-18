# 斐波那契时间带策略

## 概述

该策略是 MetaTrader "Fibonacci Time Zones" 专家的 StockSharp 移植版本。我们保留了原始脚本的思路：使用更高周期的 MACD 动量确认、布林带离场以及复杂的资金管理模块。所有控制逻辑均基于高层 API 重写，策略同时订阅交易周期与慢周期蜡烛，并通过 `Bind` / `BindEx` 回调直接获取指标值。

## 核心逻辑

1. **动量过滤**：计算一个可配置的月度 MACD。MACD 上穿信号线时记录做多意向，下穿时记录做空意向，真正的建仓在下一根交易蜡烛完成后进行，从而避免一次交叉触发多次下单。
2. **入场执行**：每个信号按照参数发送多笔市价单，若存在反向持仓会先行平仓。
3. **离场机制**：
   - **布林带退出**：多单触碰上轨、空单触碰下轨时离场。
   - **传统止盈止损**：将点数转换为价格距离，并通过 `StartProtection` 设置固定止损、止盈和跟踪止损。
   - **保本移动**：价格运行指定点数后，将止损移动到建仓价附近，若回撤到该价位则立即退出。
   - **收益回撤保护**：实时跟踪总盈亏，浮动盈利达到阈值后开始跟踪，一旦回撤超过限定值即全部平仓。
   - **权益目标**：可选的绝对收益或收益率目标，达到后立即关闭所有仓位。

## 参数说明

- `UseTakeProfitMoney`、`TakeProfitMoney`：累积盈亏达到指定货币金额时全部平仓。
- `UseTakeProfitPercent`、`TakeProfitPercent`：按初始权益百分比触发的总盈亏目标。
- `EnableTrailingProfit`、`TrailingTakeProfitMoney`、`TrailingStopLossMoney`：启用收益回撤保护的阈值与允许回吐的金额。
- `UseStop`、`StopLossPips`、`TakeProfitPips`、`TrailingStopPips`：传统止损、止盈与跟踪止损的点数设置。
- `UseMoveToBreakEven`、`WhenToMoveToBreakEven`、`PipsToMoveStopLoss`：保本移动所需的触发距离与偏移量。
- `NumberOfTrades`：每个信号发出的市价单数量，用于模拟原专家可以分批加仓的能力。
- `CandleType`、`MacdCandleType`：交易管理周期以及 MACD 使用的慢周期。

## 与原始 EA 的差异

- 未移植图表按钮与斐波那契时间带图形，仅保留交易逻辑。
- 原 EA 通过点击按钮手动下单；移植版本在 MACD 交叉时自动执行，方便回测与自动化运行。
- 账户相关函数改为使用 StockSharp 的 `Portfolio` 与 `PnL` 信息。

## 使用建议

1. 启动前设定合适的蜡烛类型。默认值为 15 分钟交易图配合月度 MACD 过滤。
2. 根据标的的最小跳动单位调整各项点数，策略内部会利用 `Security.PriceStep` 自动换算成价格差。
3. 如果希望保留更多主观干预，可以关闭资金类目标，仅使用布林带离场规则。
