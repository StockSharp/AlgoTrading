# 跨品种信号策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图同步已完成的四小时TONUSDT@BNBFT与BTCUSDT@BNBFT K线。TONUSDT提供20周期变化率信号，BTCUSDT提供自身的20周期简单移动平均线过滤条件。图表应在Strategy Security设为BTCUSDT@BNBFT时运行；内部数值`0/1`空仓/多头锁存器、共享入场许可状态和成交驱动的保护共同管理仅做多的敞口。

![schema](schema.svg)

## 策略概览

- 两个独立的品种变量仅配置TONUSDT与BTCUSDT四小时K线订阅。已完成K线在决策前对齐，因此TON动量值与BTC趋势过滤条件始终属于同一个同步区间。
- TON ROC(20)高于零时为看涨状态，等于或低于零时构成离场条件。BTCUSDT的Close不低于SMA(20)时允许入场，Close低于SMA(20)时构成离场条件。
- 多头入场要求四个条件同时成立：`TON ROC(20) > 0`、`BTC Close >= BTC SMA(20)`、内部状态锁存器显示空仓且共享`Cooldown is ready`状态允许入场。外部组合的AND随后触发数量1的NoCondition市价买入。
- 入场操作清除共享入场许可并启动Entry Cooldown N，自主信号卖出清除同一许可并启动Signal-exit Cooldown N。对应计时器在八对同步K线后恢复许可；代次检查会在较新重置后抑制旧计时器的完成事件。离场不等待该状态，保护离场也不会重置它。
- BTCUSDT市价买入成交把锁存器切换到多头，并启动2%止盈和固定、非移动的2.5%止损。自主平仓成交或Take/Stop激活与成交会把锁存器切回空仓。策略不会建立空头仓位。

## 入场与出场规则

- **做多入场**: 在一对同步且已完成的四小时K线上，当TON ROC(20)高于零、BTC Close不低于BTC SMA(20)、锁存器显示空仓且共享`Cooldown is ready`状态允许入场时，外部入场AND触发数量1的NoCondition市价买入。该操作交易所选Strategy Security，因此它必须设为BTCUSDT@BNBFT并与Traded Security K线参数一致。
- **做空入场**: 没有空头入场。自主卖出使用ReduceOnly、MarketOrder和数量1，因此只能减少所选Strategy Security的敞口；Take和Stop保护同样用于关闭多头敞口。
- **离场**: 锁存器显示多头时，`TON ROC(20) <= 0`或`BTC Close < BTC SMA(20)`会触发ReduceOnly市价卖出，且无需等待共享冷却状态。2%止盈或2.5%固定止损也可以通过市价单关闭敞口；止损不会移动。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | 仅用于交易品种K线订阅的品种。所选Strategy Security必须同样设为BTCUSDT@BNBFT，因为操作和策略成交使用Strategy Security。 |
| Signal Security | TONUSDT@BNBFT | 仅用于信号品种K线订阅的品种；其动量参与信号，但没有操作直接使用该变量下单。 |
| BTC Candles Series | 04:00:00 | 已完成的四小时BTCUSDT K线序列，用于Close、SMA(20)、状态决策和图表。 |
| TON Candles Series | 04:00:00 | 已完成的四小时TONUSDT K线序列，用于ROC(20)和同步决策。 |
| BTC SMA Length | 20 | 根据已完成BTCUSDT K线计算SimpleMovingAverage的周期。 |
| TON ROC Length | 20 | 根据已完成TONUSDT K线计算RateOfChange的周期。 |
| ROC Threshold | 0 | 用于区分正动量入场状态和非正信号离场状态的零水平。 |
| Entry Cooldown N | 8 | 入场操作计时器在恢复共享入场许可之前计数的同步K线对数量。 |
| Signal-exit Cooldown N | 8 | 自主信号离场计时器在恢复共享入场许可之前计数的同步K线对数量。 |
| Order Volume | 1 | 所选Strategy Security的NoCondition市价买入与ReduceOnly市价卖出共同使用的固定数量。 |
| Take Profit | 2% | 相对于入场成交价触发止盈保护的上涨百分比。 |
| Stop Loss | 2.5% | 相对于入场成交价触发止损保护的下跌百分比。 |
| Trailing Stop Loss | false | 已禁用，因此2.5%止损保持固定，不随有利价格移动。 |
| Use Market Orders | true | 已启用，因此止盈和止损通过市价单平仓。 |

## 图表详情

- 两个独立的品种[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)模块只连接TONUSDT@BNBFT与BTCUSDT@BNBFT的独立K线订阅。订单操作和策略成交使用Strategy Security；还应在那里选择BTCUSDT@BNBFT，使其与Traded Security K线参数一致。
- 两个[K线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)模块只输出已完成的四小时K线。[同步](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/sync.html)模块以`04:00:00`间隔配对两路数据，再将任一品种送入决策链。
- 一个[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块计算TONUSDT的RateOfChange 20，另一个计算BTCUSDT的SimpleMovingAverage 20。比较模块表示ROC的正值与非正值状态，以及BTC Close位于均线上方或下方的关系。
- 一个数值Unit[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)充当状态锁存器：`0`表示空仓，`1`表示多头。Buy MyTrade写入`1`；自主卖出的MyTrade以及Take/Stop激活和MyTrade事件写入`0`。逻辑条件模块把该状态与同步信号组合。
- 两个按操作区分的N值计时器分别保存值为8的Entry Cooldown N和Signal-exit Cooldown N。任一操作都会清除同一个共享入场许可；其计时器可在八对同步K线后恢复`Cooldown is ready`，而代次检查会拒绝旧计时器的过期完成事件。外部空仓入场AND只有一个冷却输入，并触发配置为NoCondition、MarketOrder、数量1的买入[修改仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)操作；信号离场不经过该输入，触发单独的ReduceOnly、MarketOrder、数量1卖出。
- [仓位保护](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)同时接收买入和自主平仓成交：买入成交启动Take Profit `2%`与固定Stop Loss `2.5%`，平仓成交清除过期保护状态。Trailing Stop Loss为`false`，Use Market Orders为`true`。图表接收两路同步K线、BTC SMA(20)、TON ROC(20)、Take和Stop订单流，以及策略成交提供的全部BTC成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
