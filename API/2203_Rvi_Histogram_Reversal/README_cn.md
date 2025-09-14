# RVI Histogram Reversal 策略

## 概述

该策略基于 RVI（相对活力指数），当指标离开超买或超卖区域，或当 RVI 与其信号线交叉时开仓。支持两种信号模式：

- **Levels** – 当 RVI 穿越预设上限或下限时触发。
- **Cross** – 当 RVI 与其信号线交叉时触发。

这是一个逆势策略：如果 RVI 高于上限后回落，则开多单；如果 RVI 低于下限后上穿，则开空单。

## 参数

| 名称 | 说明 |
| --- | --- |
| `RviPeriod` | RVI 计算周期。 |
| `HighLevel` | RVI 上限。 |
| `LowLevel` | RVI 下限。 |
| `Mode` | 信号模式（`Levels` 或 `Cross`）。 |
| `EnableBuyOpen` | 允许开多。 |
| `EnableSellOpen` | 允许开空。 |
| `EnableBuyClose` | 允许平多。 |
| `EnableSellClose` | 允许平空。 |
| `CandleType` | K 线时间框架。 |

## 工作原理

1. 在每根完成的 K 线上计算 RVI 及其简单移动平均线。
2. 根据所选模式，检测：
   - RVI 是否离开极值区域，或
   - RVI 是否与信号线交叉。
3. 出现做多信号时平掉空头并开多；出现做空信号时平掉多头并开空。

默认时间框架为四小时。

## 备注

- 使用市价单执行。
- 如有需要，可另行加入止损止盈管理。
