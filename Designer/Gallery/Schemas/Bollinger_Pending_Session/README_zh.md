# 带会话订单生命周期的布林带突破
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图把双向布林带突破展开为可见的挂单生命周期。完成K线收在Bollinger Bands(20, 1)之外时，在被突破的带线上登记入场；成交后在移动中线登记反向限价，Order replacing持续跟随中线，07:00-20:00会话结束时则批量撤单并清空仓位。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟K线驱动周期20、宽度1的布林带，收盘价同时进入上下突破比较。
- Working time仅在07:00至20:00允许新入场，共用的Position == 0门控明确将图简化为仅空仓入场。
- 确认上破后在上轨登记买入限价；确认下破后在下轨登记镜像卖出限价。
- 入场成交的实际Trade.Volume决定反向中线退出数量，避免用固定常量替代部分或非默认成交量。
- Combination保存Order replacing返回的最新订单；退出成交或工作时段结束会撤销所有剩余活动订单。

## 入场与出场规则

- **做多入场**: Working time内，完成收盘价高于上轨且Position == 0时，在上轨值登记买入限价。
- **做空入场**: Working time内，完成收盘价低于下轨且Position == 0时，在下轨值登记卖出限价。
- **离场**: 入场成交后，以准确成交量在布林中线登记反向限价，并随每个新中线值移动。退出成交会清理余单；07:00-20:00之外批量撤单，Modify position以市价关闭任何多仓或空仓。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 图中完成K线周期；为月度回放从C#默认四小时适配为五分钟。 |
| Bollinger Period | 20 | 布林带Length，也是实际C#策略参数。 |
| Bollinger Width | 1 | 布林带标准差倍数，也是实际C#策略参数。 |
| Session Start | 07:00:00 | Working time起点，来自源README而非C#构造函数。 |
| Session End | 20:00:00 | Working time终点；窗口外撤单并清空仓位。 |
| Order Volume | 1 | 每张入场单数量；退出数量取自实际入场成交。 |

## 图表详情

- 可执行C#使用四小时K线。五分钟是明确的回放适配，使一个月内有足够的完整指标值和突破事件通过画廊验收。
- C#构造函数只有BandPeriod、BandWidth和CandleType三个参数。Session Start与Session End来自相邻README，由Working time实现；图中另公开订单数量。
- C#多头条件为Position <= 0、空头为Position >= 0，并用市价单关闭和反转反向仓位。本教学图有意只在Position == 0时入场，不实现反转。
- 执行方式也经过适配：源代码市价进出，图中在突破带线挂入场限价并维护中线退出限价。入场单可能等待回撤，并不保证立即成交。
- Order replacing返回新的订单对象，因此每侧把原订单和替换输出汇入Combination<Order>，不会把输出回接到自身输入。
- 回放验收品种没有价格步长，因此关闭价格收缩；指标计算出的价位不变。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
