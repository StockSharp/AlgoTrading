# ExpertClor 2MA Stop ATR 策略

## 概述

ExpertClor 2MA Stop ATR 策略来源于 MetaTrader 4 智能交易系统 `ExpertCLOR_2MAwampxStATR_v01`，原版程序只负责管理已经存在的仓位。移植到 StockSharp 后保持相同理念：策略本身不会开仓，只会在指定的周期上跟踪当前仓位，并在满足原始条件时发出离场指令。

## 核心逻辑

1. **均线交叉离场**  
   在每根收盘的K线中计算两条可配置的移动平均线。当快线从上向下穿越慢线时，平掉多头仓位；当快线从下向上穿越慢线时，平掉空头仓位。判断时使用最近两根完成的K线，以复刻 MQL 中的交叉检测方式。
2. **ATR 追踪止损**  
   原代码中的自定义指标 `StopATR_auto` 用内置的 Average True Range 进行替代。候选止损计算为 `Close ± ATR × Target`，只有在等待 `AtrShiftBars` 根完整K线之后，候选值才会提升为真实止损，从而模拟 MQL 参数 `CountBarsForShift` 带来的延迟效果。止损只会朝着盈利方向移动。
3. **移动止损至保本位置**  
   当价格向持仓方向移动 `BreakevenPoints × PriceStep` 后，保护止损被上调至入场价加一个价格步长（多头）或下调至入场价减一个价格步长（空头），与原版策略让止损略微锁定利润的做法一致。
4. **仓位方向感知**  
   一旦仓位方向发生变化或回到零，内部的追踪状态会被清空，以避免在下一笔交易中使用过期的止损水平。

## 指标

- 快速移动平均线：可设置周期、类型（SMA、EMA、SMMA、WMA）以及价格类型（收盘价、开盘价、最高价、最低价、Typical、Median、Weighted）。
- 慢速移动平均线：拥有与快线相同的配置项。
- Average True Range（ATR）：用于构建 StopATR_auto 风格的追踪通道。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `MaCloseEnabled` | 是否启用均线交叉离场。 | `true` |
| `AtrCloseEnabled` | 是否启用 ATR 追踪止损。 | `true` |
| `FastMaPeriod` | 快速均线周期。 | `5` |
| `FastMaMethod` | 快速均线类型（Simple、Exponential、Smoothed、Weighted）。 | `Exponential` |
| `FastPriceType` | 快速均线使用的价格类型。 | `Close` |
| `SlowMaPeriod` | 慢速均线周期。 | `7` |
| `SlowMaMethod` | 慢速均线类型。 | `Exponential` |
| `SlowPriceType` | 慢速均线使用的价格类型。 | `Open` |
| `BreakevenPoints` | 触发保本移动所需的价格步数。 | `15` |
| `AtrShiftBars` | 在收紧 ATR 止损前需要等待的完整K线数量。 | `7` |
| `AtrPeriod` | ATR 平滑周期。 | `12` |
| `AtrTarget` | ATR 乘数，用于计算追踪止损。 | `2.0` |
| `CandleType` | 用于所有计算的K线类型。 | `5 分钟` |

## 使用说明

- 将策略附加到由其他系统负责开仓的标的上，ExpertClor 2MA Stop ATR 只会发送市价平仓指令。
- 需要与 `CandleType` 一致的完结K线数据，否则逻辑无法对齐原版顾问。
- 如果交易品种没有提供 `PriceStep`，策略会退回到数值 `1` 以确保保本逻辑仍然可用。
- ATR 追踪止损要等指标形成后才会生效；在此之前只有均线交叉和平移至保本能够触发。
- 目前仅提供 C# 版本，暂无 Python 实现。

## 转换说明

- StopATR_auto 指标通过内置 ATR 与 `AtrShiftBars` 延迟机制实现，以模拟原版的 `CountBarsForShift` 参数。
- MQL 中通过 `OrderModify` 调整止损的流程被替换成当价格触及模拟的止损水平时调用 `ClosePosition()`。
- 代码中加入了详细的英文注释，方便后续维护和扩展。
