# EMA交叉追踪限价入场图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图保留Franks4HourLimitOrdersStrategy的EMA(12)/EMA(26)交叉与Momentum(10)确认，但显式展示执行：限价从信号K线收盘价开始，在方向仍有效时由Order replacing跟随后续收盘价，并在反向交叉时撤销。

![schema](schema.svg)

## 策略概览

- 已完成K线输入两条指数移动平均和Momentum；只有EMA顺序真实改变时Crossing才发出信号。
- 向上交叉要求Momentum为正且Position <= 0；向下交叉要求Momentum为负且Position >= 0。
- Order registering在信号收盘价放置第一张限价单，并关闭价格步长收缩。
- Combination保存每次Order replacing返回的最新订单，因此更新和撤销始终针对当前订单对象。
- 只有EMA与Momentum方向持续有效时才允许替换，避免无条件反复撤挂。

## 入场与出场规则

- **做多入场**: EMA(12)上穿EMA(26)，Momentum为正，Position为空或空头时，在收盘价挂买单。数量为abs(Position)+1，把源码的平空与开多两张市价单合并为一次净限价反转。
- **做空入场**: EMA(12)下穿EMA(26)，Momentum为负，Position为空或多头时，在收盘价挂卖单，并使用相同的净反转数量。
- **离场**: 反向EMA交叉撤销待成交订单，并可能提交对向订单。成交持仓还使用图中新增的1%止损和3%止盈；保护成交会撤销剩余待处理订单引用。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 00:05:00 | 已完成K线周期；图库回放使用五分钟，C#默认四小时。 |
| Fast EMA Length | 12 | 快速ExponentialMovingAverage包含的数值数量。 |
| Slow EMA Length | 26 | 慢速ExponentialMovingAverage包含的数值数量。 |
| Momentum Length | 10 | Momentum包含的数值数量，其符号用于确认交叉。 |

## 图表详情

- C#源码使用市价单且没有挂单管理；限价注册、替换和撤销是本图有意展示的执行改造。
- 未成交限价只在EMA顺序、Momentum符号和持仓方向仍允许该设置时移动到新收盘价。
- 源码默认四小时。由于一个月H4历史仅勉强形成EMA(26)，示例默认五分钟；可把参数恢复为04:00:00。
- 基础数量1以及1%止损/3%止盈的Position protection是固定图表附加项，不是策略构造参数。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
