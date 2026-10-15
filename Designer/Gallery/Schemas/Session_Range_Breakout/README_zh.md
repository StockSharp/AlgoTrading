# 时段区间突破策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在 UTC 日间交易时段交易 BTCUSDT 对滚动八小时区间的突破。已完成的一小时 K 线定义价位并驱动决策，一个共用的每日 Flag 只放行当天首个方向候选，晚间平仓分支则把图中建立的仓位恢复为零。

![schema](schema.svg)

## 策略概览

- 每根已完成的一小时 K 线先向前偏移一个周期，再进入仅输出已形成值的 Highest 8 和 Lowest 8 指标。因此，每根新 K 线上的两个价位都代表紧邻的前八个完整小时，并会持续滚动，而不是全天固定不变。
- 公共 Time 时钟在 UTC 00:00:00 至 07:59:59 激活每日重置路径。K 线开盘时间驱动 08:00:00 至 19:59:59 的交易窗口以及 20:00:00 至 23:59:59 的平仓窗口；这些设置分别实现半开区间 `[08:00, 20:00)` 和 `[20:00, 24:00)`。
- 在交易窗口内，严格的 `Close > High` 与 `Position <= 0` 构成长仓候选，严格的 `Close < Low` 与 `Position >= 0` 构成空仓候选。收盘价等于任一边界时不会触发入场。
- 两个方向候选共用一个 Flag，因此每个 UTC 日只有首个合格的做多或做空候选可以入场。入场数量为 `Base Volume + abs(Position)`：空仓时建立一个单位的仓位，持有反向一个单位时则通过一笔市价单完成平仓并反转。
- 在平仓窗口内，正仓位发送一笔基础数量的市价卖单，负仓位发送一笔基础数量的市价买单。图中没有止损或止盈模块；图表显示 K 线、滚动 Highest 与 Lowest 价位以及四路 MyTrade。

## 入场与出场规则

- **做多入场**: UTC 08:00:00 至 19:59:59，当已完成 K 线相对于前八小时满足 `Close > Highest(8)`，仓位快照为 `<= 0`，且共用的每日 Flag 可用时，该图以 `1 + abs(Position)` 的数量提交 NoCondition 市价买单。
- **做空入场**: UTC 08:00:00 至 19:59:59，当已完成 K 线相对于前八小时满足 `Close < Lowest(8)`，仓位快照为 `>= 0`，且共用的每日 Flag 可用时，该图以 `1 + abs(Position)` 的数量提交 NoCondition 市价卖单。
- **离场**: UTC 20:00:00 至 23:59:59，仓位为正时卖出 Base Volume 1，仓位为负时买入 Base Volume 1。在正常运行中，入场只会形成 `+1` 或 `-1` 的敞口，因此固定平仓数量会把仓位恢复为零。图中未连接止损、止盈或其他保护。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 01:00:00 | BTCUSDT 的一小时时间框架；只有已完成 K 线才会进入滚动区间、时段检查、仓位快照和决策链。 |
| Range Length | 8 | 两个仅输出已形成值的 Highest 与 Lowest 指标使用的已偏移完整 K 线数量；当前 K 线不参与计算。 |
| Reset Window | 00:00:00–07:59:59 UTC | 公共 Time 时钟在交易时段开始前重置共用每日 Flag 的 UTC 区间。 |
| Trade Window | 08:00:00–19:59:59 UTC | 入场候选的 UTC 包含式配置边界；按小时 K 线开盘时间等价于半开区间 `[08:00, 20:00)`。 |
| Close Window | 20:00:00–23:59:59 UTC | 平仓的 UTC 包含式配置边界；按小时 K 线开盘时间等价于半开区间 `[20:00, 24:00)`。 |
| Base Volume | 1 | 空仓入场时使用的单位数量；反转时与 `abs(Position)` 相加，并原样提供给两笔晚间平仓单。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)模块输出已完成的一小时 BTCUSDT K 线。[前值](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)模块设置 Shift 1，从区间计算中排除当前决策 K 线。
- 两个仅输出已形成值的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块根据偏移后的 K 线流计算 Highest 8 和 Lowest 8。输出每个完整小时更新一次，构成前八小时的滚动通道。
- 公共 Time 流驱动 UTC 00:00:00–07:59:59 的重置[工作时间](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)模块。K 线流直接驱动交易和平仓工作时间模块，因此这些决策使用每根 K 线的 OpenTime。
- 每根决策 K 线都会采样当前[仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)。[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)和[逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)模块组合严格突破、时段、仓位方向与共用 Flag 检查。
- 入场数量运算计算 `Base Volume + abs(Position)`。两个入场[修改仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块提交 NoCondition 市价单，从空仓建立一个单位的多仓或空仓，或者用一笔订单完全反转反向的一个单位仓位。
- 重置窗口为每个 UTC 日恢复一个共用 Flag，首个被接受的做多或做空候选会消耗它。平仓窗口内，正仓位和负仓位的独立分支发送固定 Base Volume 1 的市价单，关闭图中正常入场路径建立的 `±1` 敞口。
- [图表面板](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html)接收已完成 K 线、Highest 8、Lowest 8，以及做多入场、做空入场、多仓平仓和空仓平仓模块的 MyTrade 输出。图中没有止损或止盈元素。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
