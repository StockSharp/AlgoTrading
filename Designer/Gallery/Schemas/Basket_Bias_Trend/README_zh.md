# 单品种SMMA趋势偏向策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

尽管保留了旧的图库名称，该图并不是篮子策略。它遵循当前VectorStrategy代码：单一策略证券、四小时K线上的快慢平滑移动平均、净持仓反转，以及按绝对货币金额计算的浮动盈亏退出。

![schema](schema.svg)

## 策略概览

- 已完成的四小时K线输入SMMA(3)和SMMA(7)，两者的相对位置定义多头或空头偏向。
- 一次性Flag启动N values，在前八根已完成K线期间禁止交易。
- 多头偏向在Position <= 0时允许买入，空头偏向在Position >= 0时允许卖出。
- 订单数量为abs(Position)加基础数量，把源码的平仓与开仓两单合并为一次净反转。
- 未实现盈亏达到+5000或-300000账户货币时，P&L change关闭敞口。

## 入场与出场规则

- **做多入场**: 预热后，快速SMMA高于慢速SMMA，且Position为空或空头。Modify position发送市价买单，平掉空头并留下一个基础单位多头。
- **做空入场**: 预热后，快速SMMA低于慢速SMMA，且Position为空或多头。Modify position发送市价卖单，平掉多头并留下一个基础单位空头。
- **离场**: 相反偏向会直接反转持仓。此外，未实现盈亏达到5000或低于-300000时触发市价ClosePosition；若趋势不变，下一根合格K线可以再次入场。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 04:00:00 | 两条平滑移动平均和持仓决策使用的已完成K线周期。 |
| Fast SMMA Length | 3 | 快速SmoothedMovingAverage包含的数值数量。 |
| Slow SMMA Length | 7 | 慢速SmoothedMovingAverage包含的数值数量。 |
| MA Shift Warmup | 8 | 启用趋势入场前跳过的初始已完成K线数。 |
| Base Volume | 1 | 从空仓入场或净反转后保留的持仓数量。 |
| Profit Target, money | 5000 | 触发ClosePosition的未实现账户货币利润。 |
| Loss Limit, money | -300000 | 未实现账户货币亏损边界，应保持为负数。 |

## 图表详情

- 源码的GetWorkingSecurities只返回(Security, CandleType)，因此有意不包含Index、Sync、第二证券或篮子确认。
- ProfitPercent 0.5和LossPercent 30按测试组合1,000,000起始余额换算为+5000与-300000。
- Designer提供货币金额的未实现盈亏但不提供起始余额，因此百分比阈值改为显式账户货币值。
- 预热统计最初八根已完成K线，与源码processedBars保护一致；交易前SMMA(7)已经形成。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
