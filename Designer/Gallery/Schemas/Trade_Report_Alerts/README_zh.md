# 成交报告提醒策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图示范的是报告，而不是信号的构思。交易本身仅来自已完成五分钟K线上9周期与26周期指数移动平均线的普通交叉，围绕它的其余部分则把这些交易转化为可读的文字：每一笔自有成交在发生的瞬间生成一行日志，而每天一次由时钟驱动的分支会写出策略的已实现结果。报告一侧只读取交易一侧，从不自行下达、修改或阻止任何委托。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟K线送入9周期的快速 ExponentialMovingAverage 和26周期的慢速均线。交叉（Crossing）模块把这一对均线归结为单一事件：快线上穿慢线时输出 `true`，下穿时输出 `false`，其余时间不输出任何内容。
- 持仓通过一次由K线触发的快照，每根K线读取一次，三次与零的比较把它描述为空仓、多头或空头。每个决策都基于该快照，因此在K线中途到达的成交无法重新打开已经作出的决策。
- 在空仓状态下，向上交叉开多，向下交叉开空。两个入场模块都带有开仓（Open-position）条件，因此只要持有任何仓位它们就保持沉默，不会把委托叠加在委托之上。
- 在持仓状态下，相反方向的交叉通过平仓（Close-position）模块将其了结，该模块的委托数量取自持仓本身。这笔平仓的委托事件随后触发新方向上的入场，因此反手被写成两个明确的步骤，而不是一张超额的委托。
- 一个对外公开的 Volume 值供全部四个入场模块使用；两个平仓模块不接收数量，因为平仓模块本身已经知道未平仓的数量。
- 策略成交（Strategy trades）模块拾取策略的每一笔自有成交，并经由字符串格式化（String formatter）送入日志通知（Log notification），因此每次成交都会留下一行包含方向、数量、合约与价格的记录。
- 第二个分支按时钟而不是按行情报告。当前时间（Current time）送入两个工作时间（Working time）窗口——一个是中午前后的报告窗口，另一个是刚过午夜的重置窗口——而置于两者之间的标志（Flag）把整个报告窗口转换为每天恰好一次的脉冲。
- 这一次脉冲把策略的已实现结果从一个保存最后所赋值的变量中释放出来，格式化后写入日志。由于该变量从零开始，即使某一天完全没有成交，状态行仍会出现。

## 入场与出场规则

- **做多入场**: 当K线时刻的快照显示为空仓时，快速指数均线上穿慢速指数均线，会按 Volume 发出市价买入委托。如果此时持有的是空头仓位，同一次交叉会先将其全部平掉，随之产生的平仓委托立即触发多头入场，因此方向在同一根K线之内完成切换。
- **做空入场**: 当K线时刻的快照显示为空仓时，快速指数均线下穿慢速指数均线，会按 Volume 发出市价卖出委托。如果此时持有的是多头仓位，同一次交叉会先将其全部平掉，随之产生的平仓委托立即触发空头入场。
- **离场**: 图中没有止损、止盈或保护模块：仓位一直持有到出现相反方向的交叉，由平仓（Close-position）模块按持仓推算数量将其完全了结。报告分支只观察成交与盈亏，从不发出、替换或撤销委托，因此关闭这些通知也不会改变交易行为。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | K线序列的时间框架；只有已完成的K线才驱动指标以及建立在其上的每个决策。 |
| Fast EMA Length | 9 | 快速 ExponentialMovingAverage 的周期；它是交叉这一对均线中较快的一半。 |
| Slow EMA Length | 26 | 慢速 ExponentialMovingAverage 的周期；它是交叉这一对均线中较慢的一半。 |
| Volume | 1 | 提供给四个入场模块的数量。两个平仓模块忽略它，其数量取自未平仓位。 |
| Report Window Begin | 12:00:00 | 每日报告窗口的起点。落入其中的第一个时刻会发出状态报告。 |
| Report Window End | 12:05:00 | 每日报告窗口的终点。它只需宽到足以让时钟落入其中一次；无论宽度多少，标志都会把报告保持为一行。 |
| Day Reset Begin | 00:00:00 | 重置窗口的起点，它清除标志并允许在次日生成新的报告。 |
| Day Reset End | 00:05:00 | 重置窗口的终点。从这一时刻到报告窗口开始之间，该分支保持沉默。 |
| Fill Report Caption | Trade report | 写在每条成交通知上的标题，日志中正是通过它来识别逐笔成交的记录行。 |
| Status Report Caption | Strategy status | 写在每日状态通知上的标题，它把这条记录与逐笔成交的记录行区分开。 |

## 图表详情

- [K线（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块只输出已完成的五分钟K线，两个[指标（Indicator）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块在其上计算9和26的 ExponentialMovingAverage 值。仅已形成（Formed-only）过滤处于关闭状态，因此两条线从回放开始即可用，并且都会绘制在图表上。
- [交叉（Crossing）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)模块只在发生交叉时触发；一个 NOT [逻辑条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)把它的向下事件变成正向触发。当前[持仓（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)保存在每根K线释放一次的[变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)中，三个[比较（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块把它变成空仓、多头和空头标志，再由四个 AND 条件与交叉组合起来。
- 六个[修改持仓（Modify position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块作用于这四个条件：两个从空仓入场、条件为 `OpenPosition`，两个平仓、条件为 `ClosePosition`，另有两个 `OpenPosition` 入场由对应平仓的委托事件触发——正是这一点让反手分两步完成。六个模块全部下达市价委托，且都不等待在线连接。
- [策略成交（Strategy trades）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)输出每一笔自有成交。一个[字符串格式化（String Formatter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)按模板 `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}` 渲染它，类型为 `Log` 的[通知（Notification）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)则以成交报告标题把它写出。
- [当前时间（Current time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)送入两个[工作时间（Working time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)检查；报告窗口设置[标志（Flag）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html)，重置窗口清除它，正是这一点把该分支限制为每天一次脉冲。该脉冲把[盈亏（P&L）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html)模块的已实现值从预设为零的变量中释放出来，第二个字符串格式化写出 `Daily status: realized result {0}`，再由第二个 `Log` 通知发布。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
