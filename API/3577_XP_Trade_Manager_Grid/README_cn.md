# XP Trade Manager Grid（C#）

## 概述

**XP Trade Manager Grid** 策略来源于 MetaTrader 4 专家顾问 `XP Trade Manager Grid.mq4`。它实现了一个双向网格：当市场价格相对于最近成交腿移动一定点数时自动补仓。原始 EA 通过前 3 单的分级止盈、在订单数量较多时的保本目标以及基于账户百分比的风控来保护利润。此 C# 版本使用 StockSharp 的高层 API（蜡烛订阅、市场价下单、策略参数）复刻上述行为。

## 交易逻辑

1. **首单** – 策略启动后立刻按照 `InitialSide` 指定的方向下第一笔市价单（默认卖出）。
2. **网格扩张** – 当收盘价距离最近同向腿超过 `StepPoints`×最小价位时，在相同方向再开一笔市价单，前提是当前总腿数小于 `MaxOrders`。
3. **前三单独立止盈** – 每个方向的第 1~3 单分别使用 `TakeProfit1Partitive`、`TakeProfit2`、`TakeProfit3` 指定的点数止盈，达到目标后通过反向市价单平仓。
4. **保本簇** – 当总腿数≥4 时，计算所有未平仓腿的加权保本价，根据多空数量选择相应的 `TakeProfitXTotal` 值并除以订单数量，得到整体止盈目标；价格触发后立即全部平仓。
5. **循环重启** – 如果首单结束且累计点数收益仍低于 `TakeProfit1Total`，则等待价格相对上一次止盈价反向移动 `TakeProfit1Offset` 点，再次开出首单。
6. **风险控制** – 根据 `RiskPercent` 计算允许的最大浮亏（账户初始权益×百分比），若实时浮动盈亏低于该值，则立即平掉所有腿。

策略内部追踪每条腿的成交量与价格，并在出现对冲持仓时优先用新成交量抵消反向腿，与原始 EA 的处理方式一致。

## 参数

- `CandleType`：驱动策略的蜡烛数据类型（默认 1 分钟）。
- `OrderVolume`：每次市价单的交易量。
- `MaxOrders`：同一时间允许存在的最大腿数（多空合计）。
- `StepPoints`：相邻腿之间的最小距离（点）。
- `RiskPercent`：允许的最大浮亏百分比。
- `TakeProfit1Total`：首单循环累计达到该点数后不再自动补单。
- `TakeProfit1Partitive`：首单止盈点数。
- `TakeProfit1Offset`：重新开首单所需的最小回撤。
- `TakeProfit2`、`TakeProfit3`：第二、第三单的止盈点数。
- `TakeProfit4Total` … `TakeProfit15Total`：当总腿数达到对应数量时的保本簇目标点数。
- `InitialSide`：初始下单方向（买/卖）。

> **提示**：所有基于点数的参数都会自动乘以交易品种的 `PriceStep`，等效于 MT4 的 `Point()`。

## 与 MT4 版本的差异

- 因 StockSharp 高层接口不提供直接修改止盈的功能，前三单使用市场单平仓而不是修改订单属性。
- 浮动盈亏通过 `PriceStep` 与 `StepPrice` 计算。若经纪商未提供正确的合约参数，需手动调整。
- MT4 界面的文本标签（Profit pips / Profit currency）未实现，策略改用内部统计判断是否重新开首单。

## 使用建议

1. 初次测试时使用较小的 `OrderVolume`，观察网格在行情中的行为。
2. 根据标的波动性调整 `StepPoints`，步长越大，网格腿数越少，回撤越低。
3. 当交易点差较大时可适当增加 `TakeProfit1Offset`，避免过早补单。
4. 保留 `StartProtection()`，确保连接异常时策略能够自恢复。

