# Zone Recovery Hedge 策略

**Zone Recovery Hedge Strategy** 是将 MetaTrader 顾问 *Zone Recovery Hedge V1* 移植到 StockSharp 平台的结果。策略在基准价位附近交替开多单和空单，只要价格穿越恢复区间就追加新的订单。每次追加的手数按照马丁格尔方式放大，直到达到设定的盈利目标或触发最大亏损限制。

## 策略原理

1. **入场过滤**：在 *RSI Multi-Timeframe* 模式下，策略会检查所选多个时间框（从 M1 到 MN1）的 RSI 数值，要求所有启用的时间框同时离开超买或超卖区。当 RSI 从超卖区向上离开时启动买入循环，从超买区向下离开时启动卖出循环。在 *Manual* 模式中没有自动信号，可通过 `StartManualMarketCycle` 或 `StartManualPendingCycle` 方法手动启动一个新的循环。
2. **初始订单**：首单手数可以使用固定值，也可以按照账户权益和计划止损距离计算的风险百分比来确定。启用 ATR 后，止损距离与恢复区宽度来自日线 ATR；否则使用经纪商点值。
3. **恢复网格**：当价格逆向运行并跨越恢复区距离时，策略会以更大的手数在相反方向开仓（可使用自定义手数列表、乘数或加法步进）。循环始终围绕基准价格交替方向，直到达到利润目标或达到最大下单次数。
4. **利润控制**：目标以账户货币计算，可使用基础止盈或恢复阶段的专用止盈（支持 ATR 比例）。`Test Commission` 参数可用来模拟手续费。当前浮盈超过目标加成本后，策略会一次性平掉整组持仓。
5. **风险保护**：若 `MaxTrades` 非零且 `SetMaxLoss` 启用，当下单数达到上限且浮动亏损低于 `MaxLoss` 时，会强制关闭所有持仓并重置循环。

> **提示：** StockSharp 默认使用净头寸模式，不支持同时持有多空对冲。本移植版本通过反转净持仓来复现恢复逻辑，但仍保持原策略的交替顺序和盈利计算方式。

## 关键参数

- **CandleType**：主交易时间框。
- **Mode**：`Manual` 仅手动入场，`RsiMultiTimeframe` 启用 RSI 自动信号。
- **RsiPeriod**、**OverboughtLevel**、**OversoldLevel** 与 `UseM1Timeframe` … `UseMonthlyTimeframe`：配置各时间框的 RSI。
- **TradeOnBarOpen**：使用前一根 K 线作为确认条件（与原版一致）。
- **RecoveryZoneSize**、**TakeProfitPoints**：未启用 ATR 时的恢复区宽度和基础止盈距离。
- **UseAtr**、**AtrPeriod**、**AtrZoneFraction**、**AtrTakeProfitFraction**、**AtrRecoveryFraction**、**AtrCandleType**：ATR 动态设置。
- **UseRecoveryTakeProfit**、**RecoveryTakeProfitPoints**：恢复阶段使用的专用止盈距离。
- **MaxTrades**、**SetMaxLoss**、**MaxLoss**、**TestCommission**：限制最大单数、最大亏损并模拟手续费。
- **RiskPercent**、**InitialLotSize**、**LotMultiplier**、**LotAddition**、`CustomLotSize1` … `CustomLotSize10`：控制每一步的下单手数。
- **UseTimer**、**StartHour**、**StartMinute**、**EndHour**、**EndMinute**、**UseLocalTime**：交易时间窗口。
- **PendingPrice**：`StartManualPendingCycle` 使用的参考价格。

## 使用建议

- 数据源需要提供所有被选中的 RSI 时间框。若无高阶数据，可由基础时间框自动聚合生成。
- 在手动模式下，调用 `StartManualMarketCycle(true/false)` 可立即按市价开启买入或卖出循环，`StartManualPendingCycle` 则从自定义价位启动循环。
- 使用风险百分比计算手数时，百分比会像原版 EA 一样被限制在 10% 以内。
- 为了正确计算盈利，需要从连接器获得 `PriceStep` 和 `StepPrice` 信息。

## 与 MetaTrader 版本的差异

- 未移植 MT4 中的图形面板、按钮和利润线，功能通过参数与公开方法提供。
- 未模拟点差成本，只有 `TestCommission` 会在利润目标中扣除。
- 由于采用净头寸模式，相反方向的订单会相互抵消，但恢复步骤和手数扩张逻辑保持一致。
