# Early Bird Range Latch 策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在价格方向与 EMA 20 一致时，交易对前一根五分钟蜡烛极值的严格突破。UTC 日锁存器每天最多接受一个新仓位，当前 ATR 14 决定止损与目标边界。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟蜡烛提供当前收盘价、前一根蜡烛的最高价和最低价、EMA 20 以及 ATR 14。Previous value 方块只将 High 和 Low 数据流后移，因此决策不会拿蜡烛与自身极值比较。
- 多头条件为 `Close > previous High` 且 `Close > EMA 20`；空头条件为 `Close < previous Low` 且 `Close < EMA 20`。所有比较均为严格比较，相等不会形成信号。
- Time 方块驱动 00:00:00 至 00:04:59 UTC 的固定每日重置区间。蜡烛时间驱动 00:05:00 至 23:59:59 的固定入场区间，一个共享 Flag 在每次重置后只放行第一个合格的方向信号。
- 被接受的信号将当前收盘价保存为入场价，并且仅在仓位快照为零时以市价开仓一单位。仓位退出后锁存器仍保持占用，直到下一次 UTC 每日重置才允许再次入场。
- 在之后每根已完成蜡烛上，公式都使用保存的入场价和当前 ATR 重算四条边界：多头止损和目标为 `entry − 1.5×ATR` 与 `entry + 2.5×ATR`，空头则交换加减号。触及任一边界时，对应的 ReduceOnly 市价动作平仓。

## 入场与出场规则

- **做多入场**: EMA 20 形成后，在 00:05:00 至 23:59:59 UTC 之间，当仓位为零、`Close > previous High`、`Close > EMA 20` 且每日 Flag 可用时，提交一单位的 OpenPosition 市价买单。
- **做空入场**: EMA 20 形成后，在 00:05:00 至 23:59:59 UTC 之间，当仓位为零、`Close < previous Low`、`Close < EMA 20` 且每日 Flag 可用时，提交一单位的 OpenPosition 市价卖单。
- **离场**: 多头在 `Close ≤ entry − 1.5×current ATR` 或 `Close ≥ entry + 2.5×current ATR` 时提交 ReduceOnly 市价卖单。空头在 `Close ≥ entry + 1.5×current ATR` 或 `Close ≤ entry − 2.5×current ATR` 时提交 ReduceOnly 市价买单。没有按时间退出、移动规则、反转或同日再次入场。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 驱动全部信号与风险计算的已完成蜡烛周期。 |
| EMA Length | 20 | 作为方向过滤器且仅在形成后输出的指数移动平均线周期。 |
| ATR Length | 14 | 在每根已完成蜡烛上重算且仅在形成后输出的 Average True Range 周期。 |
| Stop ATR Multiplier | 1.5 | 当前 ATR 的倍数，用于在保存的入场价不利方向设置边界。 |
| Target ATR Multiplier | 2.5 | 当前 ATR 的倍数，用于在保存的入场价有利方向设置边界。 |
| Order Volume | 1 | 两个 OpenPosition 入场和两个 ReduceOnly 出场共同使用的固定数量。 |

## 图表详情

- [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 方块输出已完成的五分钟蜡烛，并可从随附的分钟历史构建它们。
- 三个 [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) 提取 Close、High 和 Low。两个 [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) 方块对数值 High 与 Low 数据流应用 Shift 1。
- 仅在形成后输出的 [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 方块计算用于方向的 EMA 20 和用于风险距离的 ATR 14。EMA 就绪还会阻止在两个指标积累足够数据前入场。
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) 数据流送入负责重置的 [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) 方块。另一个 Working time 方块读取蜡烛时间，并直接参与两个入场条件。
- 共享 [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) 消耗 UTC 当天第一个多头或空头候选。Variable 方块保存仓位快照和被接受入场的收盘价；第二个入场价变量在每根蜡烛上重新发布保存值，供风险公式使用。
- [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)、[Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 和 [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 方块构成突破门槛与四条 ATR 边界。逐蜡烛退出 Flag 防止一次计算中多个输入更新造成重复平仓。
- 两个 OpenPosition 和两个 ReduceOnly [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 方块执行市价入场与退出。[Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) 接收蜡烛、前一 High 与 Low、EMA、ATR，以及汇集全部成交的 Combination 数据流。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
