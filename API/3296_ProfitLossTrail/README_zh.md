# ProfitLossTrailStrategy

## 概述

ProfitLossTrailStrategy 是从 MetaTrader 专家顾问 **ProfitLossTrailEA v2.30** 移植而来的风险控制工具。策略本身不会开仓，而是监控所选证券的持仓并自动执行保护性操作：

- 根据平均入场价设置初始止损和止盈；
- 当仓位获利后按设定的距离和步长跟踪止损；
- 在达到触发利润后将止损移动至保本位置；
- 支持在需要人工干预时移除既有的止损或止盈。

策略默认采用原始 EA 的“组合管理”模式：同方向的所有成交都会合并成一个篮子，持仓规模变化时自动重新计算保护价格。

## 参数说明

| 参数 | 说明 |
|------|------|
| **Manage As Basket** | 开启后（默认）每次同向成交都会刷新平均入场价并重算止损/止盈。若关闭，则保留首次成交时的保护价格。 |
| **Enable Take Profit** | 是否启用自动止盈。 |
| **Take Profit (pips)** | 止盈距离（点）。 |
| **Enable Stop Loss** | 是否启用自动止损。 |
| **Stop Loss (pips)** | 初始止损距离（点）。 |
| **Enable Trailing Stop** | 是否启用跟踪止损。 |
| **Trailing Activation (pips)** | 启动跟踪止损所需的最小浮盈（点），为 0 表示立即启动。 |
| **Trailing Stop (pips)** | 跟踪止损的基础距离（点）。 |
| **Trailing Step (pips)** | 每次收紧止损前所需的额外利润（点）。 |
| **Enable Break-Even** | 是否启用保本逻辑。 |
| **Break-Even Trigger (pips)** | 激活保本止损所需的利润距离（点）。 |
| **Break-Even Offset (pips)** | 保本时在入场价基础上增加的额外偏移（点）。 |
| **Remove Take Profit** | 设为 `true` 时清除止盈且不再自动退出。 |
| **Remove Stop Loss** | 设为 `true` 时清除止损，同时禁用跟踪与保本逻辑。 |
| **Candle Type** | 用于监控价格的 K 线类型，所有检查均在 K 线收盘后执行。 |

## 使用提示

1. 将策略附加到目标证券，并由人工或其他策略负责入场；本策略仅管理已有仓位。
2. 点值由 `Security.PriceStep` 自动推导，如需精确匹配请按照品种调整参数。
3. 当保本与跟踪止损同时启用时，保本逻辑优先执行，随后只有在新的止损价至少提升一个跟踪步长时才会继续收紧。
4. **Remove Stop Loss** 会同时关闭止损、跟踪和保本，行为与原 EA 保持一致。
5. 当保护价格被触发时，策略通过 `BuyMarket` / `SellMarket` 以市价平仓。

## 移植说明

- MetaTrader 中的 “Order_By_Order” 与 “Same_Type_As_One” 模式被整合为 **Manage As Basket** 开关；由于 StockSharp 不支持逐笔修改止损，默认采用篮子管理方式。
- 原策略中的 Magic Number 及备注过滤在 StockSharp 环境中不再需要，策略仅作用于 `Strategy.Security`。
- 原有的界面绘制、声音提醒和定时刷新被省略，StockSharp 可直接通过日志与图表获取反馈。
