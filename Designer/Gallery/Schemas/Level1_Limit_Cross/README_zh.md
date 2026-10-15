# 基于Level 1的EMA交叉限价入场图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图保留TwoDLimitsStrategy的EMA(14)/EMA(50)信号，并展示订单处理过程。每根已完成的五分钟K线对Level 1报价取样，在最优报价之外放置限价单，并在均线反向交叉时撤销未成交订单。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟K线输入快速EMA(14)和慢速EMA(50)，两个Crossing模块分别识别向上和向下交叉。
- BestBidPrice与BestAskPrice由Level 1异步到达，并在每根K线完成时锁存后再计算订单价格。
- 买单位于最优买价下方0.02%，卖单位于最优卖价上方0.02%，让Order cancellation有可观察的作用。
- 每次成交启动100根K线的N values冷却；完成前禁止新入场，也不向Position protection发送价格。
- 冷却结束后，Position protection采用0.3%止损和0.6%止盈。

## 入场与出场规则

- **做多入场**: EMA(14)上穿EMA(50)，Position为空或空头，最优买价有效且冷却完成时，在锁存买价下方挂买入限价单。数量为abs(Position)加基础数量，因此一次成交可平空并开多。
- **做空入场**: EMA(14)下穿EMA(50)，Position为空或多头，最优卖价有效且冷却完成时，在锁存卖价上方挂卖出限价单，并使用同样的净反转数量。
- **离场**: 反向EMA交叉会撤销仍有效的对侧限价单。入场成交并等待100根K线后，Position protection按0.3%止损或0.6%止盈退出；对向限价单成交也可反转持仓。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | EMA、报价取样、冷却计数和保护检查共同使用的已完成K线周期。 |
| Fast EMA Length | 14 | 快速指数移动平均包含的五分钟数据数量。 |
| Slow EMA Length | 50 | 慢速指数移动平均包含的五分钟数据数量。 |
| Quote Offset | 0.02% | 相对最优报价的百分比偏移：买单低于买价，卖单高于卖价。 |
| Base Volume | 1 | 抵消反向敞口后新开仓的基础数量。 |
| Cooldown, candles | 100 | 每次成交后重新启用入场和保护前等待的已完成K线数。 |
| Stop Loss | 0.3% | 保护止损距入场成交价的百分比。 |
| Take Profit | 0.6% | 止盈目标距入场成交价的百分比。 |

## 图表详情

- C#源码使用市价入场；本图有意改用Level 1限价单，以展示订单注册和撤销的生命周期。
- 源码的200/400个价格步长按回测中BTCUSDT价格换算为约0.3%/0.6%，并保留1:2比例。
- 源码在成交后的前100根K线不检查止损或止盈；价格门控真实再现了这一执行顺序。
- 报价锁存器将异步Level 1与K线驱动的EMA决策对齐。由于回放证券可能没有价格步长，价格收缩被关闭。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
