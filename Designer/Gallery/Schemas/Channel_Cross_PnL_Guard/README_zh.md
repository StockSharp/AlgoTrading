# 带P&L保护的通道交叉策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图使用BTCUSDT@BNBFT已完成的五分钟K线计算24根K线通道的中点，并在200根K线冷却期结束后按收盘价与中点的精确交叉交易。TONUSDT@BNBFT市场深度用于确认数据流就绪，并为未实现P&L检查提供时钟；一次性金额保护在达到任一设定阈值时请求撤销仍处于活动状态的入场订单，并关闭BTC持仓。

![schema](schema.svg)

## 策略概览

- BTC Security变量配置已完成五分钟K线订阅。市价订单、成交、平仓和P&L都属于所选的Strategy Security；必须将其设为BTCUSDT@BNBFT，以便与BTC Security一致。
- Highest(24)接收BTC K线并跟踪最高价，Lowest(24)跟踪最低价。算术中点为`(Highest + Lowest) / 2`；第一次可用决策只保存收盘价和中点、启动首个冷却期，不提交订单。
- 向上交叉要求`Previous Close <= Previous Midpoint`且`Current Close > Current Midpoint`。向下交叉要求`Previous Close >= Previous Midpoint`且`Current Close < Current Midpoint`。每根已完成BTC K线都会推进保存值，包括被冷却期拒绝的K线。
- 只有经过严格后续的200根已完成BTC K线且至少收到一个TON市场深度事件后，入场或反转才具备资格。每个被接受的交叉都会在提交市价订单之前重启200根K线延迟。
- 由成交驱动的有符号锁存器记录BTC管理状态：`-1`为空头，`0`为空仓，`1`为多头。未实现P&L达到或高于`500`，或者达到或低于`-300`时触发一次性保护；后续买入或卖出成交会重新将其设为待命。

## 入场与出场规则

- **做多入场**: 当出现精确向上交叉、有符号锁存器为空仓或空头、两个就绪门控均已打开且冷却完成时，提交市价买单。空仓入场使用Base BTC Volume，空头转多头则使用两倍数量。
- **做空入场**: 当出现精确向下交叉、有符号锁存器为空仓或多头、两个就绪门控均已打开且冷却完成时，提交市价卖单。空仓入场使用Base BTC Volume，多头转空头则使用两倍数量。
- **离场**: 符合条件的相反交叉执行常规一步反转，而不是单独平仓。P&L保护独立运行，在`P&L >= Profit Target`或`P&L <= -abs(Maximum Loss)`时触发，发送批量撤单意图和针对已保存入场订单的定向撤单请求，随后请求市价平仓。该金额平仓不会重启交叉冷却期。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| BTC Security | BTCUSDT@BNBFT | 五分钟K线订阅所用证券。请把Strategy Security设为同一值，因为订单动作、成交、平仓和P&L使用Strategy Security。 |
| TON Readiness Security | TONUSDT@BNBFT | 仅用于市场深度订阅，以打开数据流就绪门控并为P&L采样提供时钟；其报价不用于BTC订单或P&L估值。 |
| Candle Series | 00:05:00 | 已完成的五分钟BTCUSDT K线序列，用于通道、精确交叉决策、冷却计数和图表。 |
| Highest Length | 24 | Highest计算通道上边界时使用的BTC K线数量。 |
| Lowest Length | 24 | Lowest计算通道下边界时使用的BTC K线数量。 |
| Base BTC Volume | 1 | 默认市价入场数量。动作公式为`Base BTC Volume * (1 + abs(latch))`，因此空仓入场使用基础数量，反转使用两倍基础数量。 |
| Cooldown N | 200 | 初始化或交叉被接受后，下一次交叉能够提交订单前所需的严格后续已完成BTC K线数量。 |
| Profit Target | 500 | 未实现P&L达到或超过该水平时，一次性保护请求撤单和市价平仓。 |
| Maximum Loss | 300 | 正数形式的亏损幅度；保护阈值按`-abs(Maximum Loss)`计算，默认值为`-300`。 |

## 图表详情

- BTC[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)仅向已完成的[K线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)订阅提供值。TON变量仅连接[市场深度](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html)；第一个事件写入就绪锁存器，后续事件也为最新未实现P&L值提供采样时钟。
- 两个[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块接收每根BTC K线。Highest(24)、Lowest(24)、中点公式、当前值锁存器和前值锁存器，为每根已完成K线保留一次完整的收盘价与通道决策。
- Delay模块在第一次决策时启动，并在每个被接受的交叉处重启。当前K线先进入其Input，然后决策支路才运行，因此只有在后续200根已完成BTC K线之后才重新具备资格；被拒绝的交叉仍会替换保存的收盘价和中点。
- 买入和卖出[订单注册](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)模块按`Base BTC Volume * (1 + abs(latch))`提交市价订单。它们的MyTrade输出根据实际成交写入有符号状态并重新使P&L保护待命。
- P&L变动事件和TON市场深度事件对最新未实现P&L进行采样。严格阈值比较进入一个共享待命门控，因此在后续入场成交重新待命之前，达到`500`或`-300`只能产生一次保护动作。
- 保护调用[批量撤单](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html)，将保存的买卖Order引用释放给定向撤单模块，并以Base BTC Volume按市价关闭当前BTC敞口。图表接收BTC K线、Highest(24)、Lowest(24)、中点、P&L、已提交订单和全部策略成交。

## 使用方法

将 `.json` 文件导入 Designer，把 Strategy Security 设为 BTCUSDT@BNBFT，使用K线与市场深度历史在回测器中运行，然后根据自己的交易品种调整参数或模块，确认后再用于实盘。
