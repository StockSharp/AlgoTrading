# 针形K线挂单入场策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该纯多头图表在上升均线扇形中寻找较长下影线。它不在信号收盘时买入，而是在影线内部挂买入限价单；订单若在指定数量的已完成K线后仍未成交则撤销，成交后用百分比止盈止损保护，并在快速EMA跌破中速EMA时平仓。

![schema](schema.svg)

## 策略概览

- 已完成的三十分钟K线进入open、high、low和close转换器，以及只输出已形成值的EMA 6、EMA 18和SMA 50。
- 公式（Formula）计算(min(open, close) - low) / (high - low)，下影线必须超过整根K线范围的0.45。
- 趋势过滤要求EMA 6 > EMA 18 > SMA 50；信号K线最低价还要跌破EMA 6，而收盘价重新回到其上方。
- 入场门同时要求所有形态条件、空仓Position，以及自最近一次策略成交后已完成六根K线。
- 订单注册（Order registering）按low * (1 + 0.25 / 100)挂出Order Volume的买入限价单，并关闭价格缩减。
- N值（N values）从注册起计数六根已完成K线，然后触发订单撤销（Order cancellation）；订单成交（Trades for order）把成交送入保护。
- 持仓保护（Position protection）设置1.4%止盈和0.7%止损；EMA 6低于EMA 18时另行触发市价ClosePosition。

## 入场与出场规则

- **做多入场**: 当下影线占比超过0.45、EMA 6 > EMA 18 > SMA 50、最低价低于EMA 6而收盘价重回其上、Position为空仓，并且最近策略成交后至少经过六根K线时，图表在信号最低价上方0.25%注册买入限价单。只有后续价格触及并成交才会入场。
- **做空入场**: 图表没有做空入口。均线扇形转弱只用于退出，不用于建立空头。
- **离场**: 未成交限价单在六根已完成K线后撤销。已成交多头由Position protection在+1.4%或-0.7%处平仓，或在EMA 6跌破EMA 18时由市价ClosePosition平仓。该市价退出成交会返回保护模块以清除保护状态。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:30:00 | 形态、指标、订单寿命、冷却和退出所用的已完成K线周期。 |
| Fast EMA Length | 6 | 快速ExponentialMovingAverage长度，影线需刺穿且收盘需重新站上它。 |
| Medium EMA Length | 18 | 上升扇形中间ExponentialMovingAverage的长度。 |
| Slow SMA Length | 50 | 扇形底部SimpleMovingAverage的长度。 |
| Wick Share | 0.45 | 下影线占整根K线范围的最低比例。 |
| Entry Offset, % | 0.25 | 买入限价相对信号最低价上移的百分比。 |
| Order Volume | 1 | 每张挂单买入的数量。 |
| Order Life, candles | 6 | 未成交限价单被撤销前的已完成K线数。 |
| Take Profit, % | 1.4 | 相对入场成交价的有利距离（%）。 |
| Stop Loss, % | 0.7 | 相对入场成交价的不利距离（%）。 |
| Cooldown, candles | 6 | 最近策略成交后允许再次入场前的最少已完成K线数。 |

## 图表详情

- 所有价格字段、指标、状态检查和计数器共用同一条已完成三十分钟K线流，订单时间保持在交易时钟上。
- AND门组合影线大小、两项扇形比较、快速EMA的刺穿与恢复、空仓Position以及冷却就绪。
- Order registration将同一订单送往N values、Order cancellation、Trades for order和图表；订单寿命由注册事件启动并由已完成K线推进。
- Strategy trades在每次自身成交时把冷却计数归零；每根K线将其增加到设定上限，较大的初始值允许第一个设置立即生效。
- 成交后空仓过滤会阻止新入口。在较早限价单尚未成交时，新的合格K线会形成独立挂单尝试，每张订单都遵循六根K线撤销规则。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
