# Martingale VI Hybrid 策略（C#）

## 概述
Martingale VI Hybrid 策略将原始的 MetaTrader 专家顾问转换为 StockSharp 高级 API。策略采用快/慢均线过滤与 MACD 组合信号，并通过马丁格尔倍量方式分批进场。当价格按照固定点差距离逆向运动时，会继续加仓，并把所有订单的止盈统一在最新订单定义的位置。同时支持以货币金额、账户初始权益百分比以及资金回撤式移动止盈等多种总体退出条件。

## 交易逻辑
1. **信号过滤**：使用上一根 K 线的快慢 SMA 与 MACD 主线/信号线关系。若快 SMA 在慢 SMA 之上且 MACD 主线低于信号线，则触发多头循环；反之若快 SMA 在慢 SMA 之下且 MACD 主线高于信号线，则触发空头循环。
2. **初始建仓**：当出现新的循环并且没有持仓时，按照 `Initial Volume` 参数下单市场单。
3. **马丁加仓**：持仓期间跟踪最近的入场价。当价格逆势走出 `Pip Step` 点后，按照“上一单手数 × Volume Multiplier”再次下单。开放订单数量受 `Max Trades` 限制；若达到上限且 `Close Max Orders` 为真，则立即平掉全部仓位。
4. **统一止盈**：每次加仓后都根据最新入场价和 `Take Profit (pips)` 重新计算整个组合的止盈价位。当 K 线最高价（多头）或最低价（空头）触及该价位时，全部订单一并平仓。
5. **整体退出**：
   - 启用 `Use Money TP` 时，浮动盈亏达到 `Money TP` 即平仓。
   - 启用 `Use Percent TP` 时，浮动盈亏达到账户初始权益的 `Percent TP` 百分比即平仓。
   - 启用 `Enable Trailing` 时，当利润超过 `Trailing Activation` 后开始以金额为单位的移动止盈，一旦利润回撤 `Trailing Drawdown` 即平仓。

## 参数说明
| 参数 | 说明 |
|------|------|
| `Candle Type` | 指标计算所使用的主时间框架。
| `Fast MA`, `Slow MA` | 定义趋势过滤的快慢简单移动平均周期。
| `MACD Fast`, `MACD Slow`, `MACD Signal` | MACD 指标的参数设置。
| `Initial Volume` | 每个马丁循环的首单手数。
| `Volume Multiplier` | 每次加仓的手数倍数。
| `Max Trades` | 马丁循环中允许同时存在的最大订单数量。
| `Take Profit (pips)` | 每单止盈距离，最新订单决定全局止盈价。
| `Pip Step` | 价格逆势移动多少点后触发下一次加仓。
| `Use Money TP`, `Money TP` | 是否启用以及设置货币金额止盈目标。
| `Use Percent TP`, `Percent TP` | 是否启用以及设置初始权益百分比止盈目标。
| `Enable Trailing`, `Trailing Activation`, `Trailing Drawdown` | 资金型移动止盈的相关参数。
| `Close Max Orders` | 达到最大订单数时是否立即全部平仓。

## 风险控制
- 同时提供金额和百分比两种全局止盈，便于提前锁定收益。
- 资金型移动止盈可在盈利阶段限制最大回撤。
- `Max Trades` 限制了马丁格尔的加仓次数，配合 `Close Max Orders` 可以在到达阈值时强制退出，避免仓位无限放大。

## 实现细节
- 使用 StockSharp 高级 `SubscribeCandles` 订阅 API，通过 `BindEx` 绑定 MACD，同时在回调中手动处理 SMA 值。
- 根据品种的最小价格变动步长自动推导点值，兼容 5 位和 3 位报价。
- 利用 `Security.PriceStep`、`Security.StepPrice` 以及 `PositionAvgPrice` 计算浮动盈亏，需要标的提供相应元数据。
