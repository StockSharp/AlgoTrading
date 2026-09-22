# 按货币目标平仓策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在SMA(10)/SMA(30)方向逻辑外增加按货币计价的紧急退出。入场有意使用挂单，因此未实现盈利或亏损达到边界时，Mass order cancellation会在ClosePosition平仓前真正撤销工作订单。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟K线输入快慢简单移动平均；Greater和Less每根K线检查状态，而不是等待交叉事件。
- 多头状态且Position <= 0时在收盘价挂买单；空头状态且Position >= 0时挂卖单。
- 数量为abs(Position)加基础数量，用一张净订单保留源码的平仓后反向开仓。
- P&L change把策略未实现结果与账户货币+300和-150比较。
- 任一边界同时触发Mass order cancellation和市价ClosePosition。

## 入场与出场规则

- **做多入场**: 快速SMA高于慢速SMA，Position为空或空头时，在已完成收盘价挂买单，数量足以平空并留下一个基础单位多头。
- **做空入场**: 快速SMA低于慢速SMA，Position为空或多头时，在已完成收盘价挂卖单，数量足以平多并留下一个基础单位空头。
- **离场**: PnLUnreal >= 300或<= -150时撤销策略全部活动订单并市价平仓。P&L回到零后阈值信号消失，策略可以再次交易。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 两条简单移动平均和入场判断使用的已完成K线周期。 |
| Fast SMA Length | 10 | 快速SimpleMovingAverage包含的数值数量。 |
| Slow SMA Length | 30 | 慢速SimpleMovingAverage包含的数值数量。 |
| Base Volume | 1 | 从空仓入场或净反转后保留的持仓数量。 |
| Profit Target, money | 300 | 触发清算的未实现策略账户货币利润。 |
| Loss Limit, money | -150 | 未实现策略账户货币亏损边界，应保持为负数。 |

## 图表详情

- C#中的RequestCloseAll从未被调用；实际路径只按SMA状态用市价单交易。本图明确实现策略宣称的货币退出。
- 源码参数是组合权益水平且默认均为零，无法实用；图中改用策略PnLUnreal及+300/-150回放值。
- 收盘价限价入场替代源码市价单，让Mass order cancellation有工作订单可撤。回放证券可能无价格步长，因此关闭价格收缩。
- 源码清算后会调用Stop；本图有意继续运行，以便月度回测展示多次入场和退出。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
