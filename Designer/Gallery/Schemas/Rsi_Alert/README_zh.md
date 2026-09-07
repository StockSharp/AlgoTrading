# RSI 提醒策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表将 RSI 极值转化为交易和易读的提醒。它处理已完成的五分钟K线，仅在空仓时于 RSI 不高于30时买入、不低于70时卖出，并为每个已成交的入场应用百分比保护。每个被接受的信号还会捕获数值型 RSI、将其格式化并写入通知。

![schema](schema.svg)

## 策略概览

- 一条仅输出已完成K线的五分钟K线流驱动指标、持仓快照、入场决策、保护价格检查和图表。
- RelativeStrengthIndex 使用周期14。仅输出已形成值的过滤器已禁用（`IsFormed = false`），因此不会仅因指标尚未形成而抑制预热阶段的值。
- 表达式为 `a` 的 Formula 方块将 RSI IndicatorValue 转换为供比较和消息使用的数值。
- 数值型 RSI 与超卖和超买水平进行比较。每个方向信号都与当前K线求值期间取得的持仓快照汇合，两个入场方块都使用 Open position 条件。
- 入场成交后会激活持仓保护，止盈为2%，止损为1%。被接受的入场信号还会将捕获的 RSI 值通过格式化器送入 Log 类型的通知。

## 入场与离场规则

- **做多入场**：数值型 RSI 小于或等于 Oversold Level，且持仓快照为空仓。图表按市价买入设定的交易量，并写入一条包含信号值的买入提醒。
- **做空入场**：数值型 RSI 大于或等于 Overbought Level，且持仓快照为空仓。图表按市价卖出设定的交易量，并写入一条包含信号值的卖出提醒。
- **离场**：当已完成K线的收盘价相对于入场价达到2%的止盈水平或1%的止损水平时，持仓保护会平掉交易。相反方向的 RSI 信号不会反转未平仓持仓，保护订单成交也不会在同一根K线上引发再次入场。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| RSI Period | 14 | 计算 RelativeStrengthIndex 所使用的K线数量。 |
| Oversold Level | 30 | 当图表空仓时，位于此水平或更低的 RSI 值允许做多入场。 |
| Overbought Level | 70 | 当图表空仓时，位于此水平或更高的 RSI 值允许做空入场。 |
| Take Profit | 2% | 保护性止盈相对于入场价的距离。 |
| Stop Loss | 1% | 保护性止损相对于入场价的距离。 |
| Volume | 0.01 | 入场订单量，单位为手。 |
| Candles | 00:05:00 | 五分钟K线周期；仅处理已完成K线。 |

## 图表详情

- [Candles](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 的输出首先触发当前持仓快照，然后更新 [Indicator](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)，最后更新收盘价 [Converter](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)。
- RSI 输出进入表达式为 `a` 的 [Formula](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html) 方块。其数值输出在任一阈值比较求值之前到达两个消息值锁存器。
- 两个 [Comparison](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 方块使用 `<=` 和 `>=`，将数值型 RSI 与共享的 Oversold Level 和 Overbought Level 值进行比较。
- [Position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 值由一个 [Variable](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 方块在当前K线求值期间保持，并与零进行比较。两个 [Logical condition](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 方块分别将该空仓结果与做多和做空 RSI 信号汇合。
- 两个入场 [Modify position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 方块都使用带 Open position 条件的市价单，并从同一个共享交易量值接收 `0.01`。
- 两个入场方块的 MyTrade 输出都送入 [Position protection](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)。收盘价转换器为其 Price 输入提供数值，该方块使用2%的止盈和1%的止损。
- 每个组合后的入场信号都会触发各自的 RSI 值锁存器。[String format](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) 生成 `RSI {0:0.0} <= 30 — buy` 或 `RSI {0:0.0} >= 70 — sell`，[Notification](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) 的 Log 类型方块会发布这些消息。
- Chart panel 接收已完成K线、RSI 值、两个入场成交流以及保护性离场成交。

## 使用方法

将 `.json` 文件导入 Designer，并在回测器中用历史数据运行。请在日志中查看格式化的 RSI 通知，并根据K线收盘价核对保护性离场。如果更改任一 RSI 阈值，也要更新相应的格式化模板，以确保提醒文本准确。
