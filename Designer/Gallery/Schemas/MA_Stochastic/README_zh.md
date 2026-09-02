# 均线 + 随机指标回调策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

两个方块共同做决定：SimpleMovingAverage 决定图表只允许做哪一边，StochasticK 则等待价格朝相反方向回撤后才发出订单。一旦收盘价回到均线另一侧，持仓即被交回。

![schema](schema.svg)

## 策略概览

- 方向由收盘价与 SimpleMovingAverage 的关系决定：位于均线之上只考虑做多，之下只考虑做空。
- 入场本身是逆势的：%K 线必须处于超卖区才做多、处于超买区才做空，因此图表在上升趋势中买回调、在下降趋势中卖反弹。
- StochasticK 正是原策略手工计算的 %K：最近 N 根K线上的 100 * (Close - 最低 Low) / (最高 High - 最低 Low)。
- 同一条均线也是离场线；图中没有任何止损或止盈。

## 入场与出场规则

- **做多入场**: 收盘价高于 SimpleMovingAverage，StochasticK 低于超卖水平，且当前没有持仓。订单以市价买入一手。
- **做空入场**: 收盘价低于 SimpleMovingAverage，StochasticK 高于超买水平，且当前没有持仓。订单以市价卖出一手。
- **离场**: 第一根收在均线之下的K线平掉多单，第一根收在均线之上的K线平掉空单；两个平仓方块按持仓量确定数量。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| SMA Length | 20 | 用于过滤趋势并平仓的 SimpleMovingAverage 平滑周期。 |
| %K Length | 14 | %K 线回看的K线数量。 |
| %K Oversold | 20 | 低于该水平即视为超卖，允许做多。 |
| %K Overbought | 80 | 高于该水平即视为超买，允许做空。 |
| Volume | 1 | 下单数量（手）。 |
| Candles | 00:05:00 | 整张图使用的K线周期。 |

## 图表详情

- K线方块分出三条支线：读取收盘价的转换方块、SimpleMovingAverage 和 StochasticK 指标。
- 两个比较方块把收盘价与均线对照，另两个把 %K 与阈值常量对照，还有一个把持仓与零对照。
- 每个逻辑与合并趋势条件、随机指标条件和空仓判断，然后触发只在空仓时开仓的修改持仓方块。
- 趋势比较在离场时被重复使用：允许做空的同一个信号也用于平掉多单，这让图保持精简。原策略每次交易后暂停100根K线的计数器没有对应方块，因此被省略。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
