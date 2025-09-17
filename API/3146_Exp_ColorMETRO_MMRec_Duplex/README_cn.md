# Exp ColorMETRO MMRec Duplex 策略

## 概述
本策略是在 StockSharp 平台上对 MetaTrader 5 专家顾问 `Exp_ColorMETRO_MMRec_Duplex` 的实现。原始 EA 由两个独立的 ColorMETRO 模块组成（分别负责多头和空头），并附加 MMRec 资金管理模块，在连续亏损后降低下单手数。C# 版本复刻了该结构，并使用 StockSharp 的高级 API 完成行情订阅与下单。

## 交易逻辑
- 为多头与空头分别构建一个 ColorMETRO 指标实例，每个实例都可以配置自己的 K 线类型，只管理自身方向的仓位。
- 指标输出快速与慢速两条阶梯形 RSI 通道。策略保存历史值并按照 `SignalBar` 指定的偏移读取数据，以模拟 MQL5 中的 `CopyBuffer` 行为。
- 当被检验的那根柱上快速通道从上向下穿越慢速通道、而上一根柱快速通道仍位于慢速通道之上时，多头信号触发。若当前持有空头，会先平空再开多。
- 如果上一根被检验柱的慢速通道高于快速通道，则关闭多头仓位，这与原版 EA 的退出条件一致。
- 空头规则与多头完全对称（穿越方向相反，退出条件为上一根柱快速通道高于慢速通道）。
- 仅处理已完成的 K 线；在指标给出完整的双通道值之前不会进行交易，从而重现 MetaTrader 的预热阶段。

## 资金管理（MMRec）
- `Strategy.Volume` 用作基准手数，多头与空头模块分别乘以 `LongMm`/`ShortMm` 得到下单量。
- 每次平仓后都会记录结果是否亏损（使用 K 线收盘价，与 EA 检查历史成交的逻辑一致）。
- 如果最近 `TotalTrigger` 笔交易中有不少于 `LossTrigger` 笔亏损，该模块会切换到缩减后的乘数 `SmallMm`；当亏损数量回落到阈值以下时，自动恢复默认乘数。
- 当出现反向信号时，会先结算当前仓位并更新 MMRec 计数，然后计算新方向的下单手数。

## 指标说明
- `ColorMetroMmrecIndicator` 完整移植了自定义 ColorMETRO 指标，包含基于 RSI 的阶梯包络与趋势记忆机制。
- 指标同时输出内部 RSI 值与 `IsReady` 标志，使策略能够像原始 MQL 代码一样忽略不完整的数据。

## 参数
| 分组 | 名称 | 说明 |
| --- | --- | --- |
| Long | `LongCandleType` | 多头模块使用的蜡烛类型或周期。 |
| Long | `LongTotalTrigger` | 评估 MMRec 时统计的多头历史交易数。 |
| Long | `LongLossTrigger` | 触发多头缩减乘数所需的亏损次数。 |
| Long | `LongSmallMm` | 多头在连续亏损后使用的缩减乘数。 |
| Long | `LongMm` | 多头默认乘数。 |
| Long | `LongEnableOpen` | 是否允许开多。 |
| Long | `LongEnableClose` | 是否允许平多。 |
| Long | `LongPeriodRsi` | 多头 ColorMETRO 中的 RSI 周期。 |
| Long | `LongStepSizeFast` | 多头快速通道的阶梯宽度。 |
| Long | `LongStepSizeSlow` | 多头慢速通道的阶梯宽度。 |
| Long | `LongSignalBar` | 读取指标数据时向前回看的柱数。 |
| Long | `LongMagic` | 保留的 MT5 magic number（仅用于兼容）。 |
| Long | `LongStopLossTicks` | 来自 EA 的止损距离占位参数（未在代码中执行）。 |
| Long | `LongTakeProfitTicks` | 来自 EA 的止盈距离占位参数（未在代码中执行）。 |
| Long | `LongDeviationTicks` | 来自 EA 的滑点容忍占位参数（未在代码中执行）。 |
| Long | `LongMarginMode` | 保留的资金管理模式标志（逻辑上按原始乘数运作）。 |
| Short | `ShortCandleType` | 空头模块使用的蜡烛类型或周期。 |
| Short | `ShortTotalTrigger` | 评估 MMRec 时统计的空头历史交易数。 |
| Short | `ShortLossTrigger` | 触发空头缩减乘数所需的亏损次数。 |
| Short | `ShortSmallMm` | 空头在连续亏损后使用的缩减乘数。 |
| Short | `ShortMm` | 空头默认乘数。 |
| Short | `ShortEnableOpen` | 是否允许开空。 |
| Short | `ShortEnableClose` | 是否允许平空。 |
| Short | `ShortPeriodRsi` | 空头 ColorMETRO 中的 RSI 周期。 |
| Short | `ShortStepSizeFast` | 空头快速通道的阶梯宽度。 |
| Short | `ShortStepSizeSlow` | 空头慢速通道的阶梯宽度。 |
| Short | `ShortSignalBar` | 读取指标数据时向前回看的柱数。 |
| Short | `ShortMagic` | 保留的 MT5 magic number（仅用于兼容）。 |
| Short | `ShortStopLossTicks` | 来自 EA 的止损距离占位参数（未在代码中执行）。 |
| Short | `ShortTakeProfitTicks` | 来自 EA 的止盈距离占位参数（未在代码中执行）。 |
| Short | `ShortDeviationTicks` | 来自 EA 的滑点容忍占位参数（未在代码中执行）。 |
| Short | `ShortMarginMode` | 保留的资金管理模式标志（逻辑上按原始乘数运作）。 |

## 实现说明
- 使用 `SubscribeCandles(...).BindEx(...)` 完成数据绑定，没有直接访问指标缓冲区，符合移植规范。
- EA 中的止损/止盈参数仅作为兼容字段保留，需要时可通过 `StartProtection` 或自定义风险控制模块实现。
- 两个模块共用同一标的，但维护各自的蜡烛订阅与 MMRec 计数器，完全保留原始双模块结构。
- 代码中的注释全部为英文，并且未使用诸如 `GetTrades` 的受限 API。

## 免责声明
本移植仅复刻了原始 EA 的逻辑结构，实际表现取决于数据源、经纪商以及 StockSharp 配置。请务必在历史数据和模拟环境中充分验证后再投入真实资金。
