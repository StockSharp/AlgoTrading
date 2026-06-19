# SilverTrend Duplex 策略

## 概述

**SilverTrend Duplex 策略** 是 MetaTrader 5 智能交易系统 `Exp_SilverTrend_Duplex` 的 StockSharp 移植版本。原始脚本通过两个互相独立的 SilverTrend 滤波器（多头与空头）侦测颜色从多头到空头的切换信号。本 C# 版本完整保留双通道结构，使多头与空头可以分别调整，同时利用 StockSharp 的高级 API。

策略仅在收盘后处理 K 线。多头与空头可以订阅不同的时间框架或品种；内部自定义的 `SilverTrendIndicator` 结合 Donchian 通道极值与风险系数，复刻 MQL 指标的颜色缓冲区输出。

## 交易逻辑

1. **指标重建**
   - 计算最近 `SSP` 根 K 线的 Donchian 上轨与下轨。
   - 使用风险参数（`33 - risk`）推导阈值 `smin` 与 `smax`，与原脚本完全一致。
   - 收盘价高于 `smax` 记为多头状态，低于 `smin` 记为空头状态，否则沿用上一根状态。根据开收盘关系将状态映射到 0..4 的颜色代码。

2. **信号准备**
   - 同时记录最近 `SignalBar + 1` 根已完成 K 线的颜色，用于多头与空头判断。
   - 当目标位置的颜色小于 `2` 且前一颜色大于 `1` 时触发多头入场，复现 MQL 中的 `Value[1] < 2 && Value[0] > 1` 条件。
   - 当颜色大于 `2` 且前一颜色大于 `0` 时触发空头入场，对应原脚本的 `Value[1] > 2 && Value[0] > 0`。

3. **下单执行**
   - 入场使用 `BuyMarket` 或 `SellMarket`，数量为 `Volume + |Position|`，即可在同一笔市价单内反转并建立新的方向。
   - 多头颜色升至 `> 2` 时平多，颜色降至 `< 2` 时平空。

> **注意**：原始 `TradeAlgorithms.mqh` 中的资金管理、止损、滑点设置未在此版本实现，需要通过 StockSharp 风控规则或券商参数单独配置。

## 参数说明

| 名称 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `LongCandleType` | 4 小时 K 线 | 多头 SilverTrend 所使用的数据类型。 |
| `LongSsp` | 9 | 多头滤波的回溯长度。 |
| `LongRisk` | 3 | 多头滤波的风险系数。 |
| `LongSignalBar` | 1 | 多头信号回溯的已完成 K 线数量，必须 ≥ 1。 |
| `EnableLongEntries` | true | 是否允许开多。 |
| `EnableLongExits` | true | 是否允许按指标平多。 |
| `ShortCandleType` | 4 小时 K 线 | 空头 SilverTrend 所使用的数据类型。 |
| `ShortSsp` | 9 | 空头滤波的回溯长度。 |
| `ShortRisk` | 3 | 空头滤波的风险系数。 |
| `ShortSignalBar` | 1 | 空头信号回溯的已完成 K 线数量，必须 ≥ 1。 |
| `EnableShortEntries` | true | 是否允许开空。 |
| `EnableShortExits` | true | 是否允许按指标平空。 |
| `Volume` | 1 | 入场单的基础数量。 |

## 实现细节

- 只有当指标形成并且颜色历史长度足够 (`SignalBar + 1`) 时才评估信号，对应 MQL 中的 `BarsCalculated` 检查。
- 自定义指标直接输出颜色值，通过 `Bind` 高阶 API 无需手动访问缓冲区。
- 当多头与空头使用相同时间框架时，仍会建立两个订阅，以保持参数和状态相互独立，符合原策略两个句柄的设计。
- 如需止损或利润目标，可在策略上附加 `StopLossRule`、`TakeProfitRule` 等 StockSharp 规则以补齐原有功能。

## 使用建议

- 分别优化多头与空头的 `SSP`、`Risk` 以适应不同市场波动。
- 若想完全模拟“上一根 K 线发出信号”的行为，请保持 `SignalBar = 1`；更大的数值会延迟响应。
- 建议与账户级风控或交易时段过滤器配合使用，避免在震荡行情中因频繁颜色切换而产生过多交易。
