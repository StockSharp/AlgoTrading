# 带到期限价单的加权信号篮子
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图把RSI区域投票和价格相对EMA的投票合成为−3到+3的分数。空仓时穿越阈值会在完成收盘价登记限价，Combination<Order>把准确订单交给十二根K线的N values计时器与Order cancellation，成交则启动1.2%/0.8%的Position protection。

![schema](schema.svg)

## 策略概览

- RSI低于30贡献+2，高于70贡献−2，中间区域贡献零。
- Close高于EMA(20)贡献+1，低于EMA贡献−1，Formula把两个加权投票相加。
- 当前值和Previous value比较检测新鲜的+1上穿或−1下穿，两侧都要求Position == 0。
- 买卖入场共用完成close和数量1；Order输出汇入Combination，MyTrade输出进入Position protection。
- N values从登记后计数十二根完成K线，再由Order cancellation撤销当前未成交订单。

## 入场与出场规则

- **做多入场**: 分数从低于+1变为至少+1且仓位为空时，Order registering在完成close登记买入限价。
- **做空入场**: 分数从高于−1变为至多−1且仓位为空时，Order registering在完成close登记卖出限价。
- **离场**: 已成交入场按成交价+1.2%和−0.8%保护。未成交限价作为Order对象，在N values计满十二根完成K线后交给Order cancellation。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 完成K线周期；为月度回放把C#默认60分钟适配为5分钟。 |
| RSI Length | 14 | RSI周期；可执行C#默认21，紧凑图使用14。 |
| EMA Length | 20 | EMA周期；可执行C#默认50，图中使用20。 |
| RSI Weight | 2 | RSI超卖与超买区域投票的权重。 |
| Trend Weight | 1 | close相对EMA投票的权重。 |
| Replay Signal Threshold | 1 | 回放穿越边界；蓝图值2保留为有文档的调参选择。 |
| Cancel After N Candles | 12 | 尝试撤销未成交入场前计数的完成K线值。 |
| Take Profit, % | 1.2 | Position protection相对成交价的盈利百分比。 |
| Stop Loss, % | 0.8 | Position protection相对成交价的亏损百分比。 |
| Order Volume | 1 | 每张买入或卖出限价的数量。 |

## 图表详情

- 可执行C#默认60分钟、RSI(21)、EMA(50)、阈值2行为和四根K线冷却，还计算K线方向及RSI中间区域。本紧凑图有意使用5分钟、RSI(14)、EMA(20)、两个投票且无独立冷却。
- 评审蓝图原定阈值2。只保留两个投票时，三月回放没有订单：超卖RSI通常伴随价格低于EMA，投票彼此抵消。因此透明的回放默认值改为1；参数仍公开，可恢复为2。
- 相邻README描述源EA的八个加权模式、挂单偏移、到期和保护。当前C#实际实现三个分数组并使用市价入场，没有挂单到期或保护块。
- 用close限价替代C#市价单是有意的，它让Combination、N values与Order cancellation拥有真实订单生命周期。回放品种没有价格步长，因此禁用价格收缩。
- 与C#的Position <= 0 / >= 0门控不同，本图仅空仓入场且不反转。1.2/0.8百分比保护是画廊教学风险模块，并非当前C#逻辑。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
