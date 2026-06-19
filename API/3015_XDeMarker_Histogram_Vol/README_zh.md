# XDeMarker Histogram Vol 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 原版 **Exp_XDeMarker_Histogram_Vol** 顾问迁移到 StockSharp 的高级 API。它把 DeMarker 指标转换为带权重的柱状图，使用可配置的移动平均线同时平滑指标与成交量，并在柱状图进入不同区间时做出交易决策。

逻辑保持完全对称：柱状图进入多头区间时开多，进入空头区间时开空；当出现相反信号且允许时，持仓会被关闭并立即反向。

## 核心思想

1. **成交量加权 DeMarker**
   - 按指定周期计算 DeMarker。
   - 将数值缩放到 `[-50, +50]` 区间后乘以选定的 K 线成交量。
   - 对加权后的指标以及成交量本身应用同一种移动平均。由于 StockSharp 原生只提供四种均线，因此仅支持简单、指数、平滑和加权四种类型。
2. **动态阈值**
   - `HighLevel1`、`HighLevel2`、`LowLevel1`、`LowLevel2` 四个乘数定义多头和空头阈值。
   - 阈值再乘以平滑后的成交量，成交越活跃，允许的振幅越大。
3. **状态机**
   - 每根已完成的 K 线被归类为 5 个状态之一：`0`（极强多头）、`1`（多头）、`2`（中性）、`3`（空头）、`4`（极强空头）。
   - 当最近的信号 K 线（由 `SignalBar` 指定的偏移）相对前一根发生向多头或空头区域的状态迁移时触发信号。

## 参数

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 主交易周期，默认 2 小时，与原顾问一致。 |
| `DeMarkerPeriod` | DeMarker 指标周期。 |
| `HighLevel1` / `HighLevel2` | 定义第一、第二多头阈值的正向乘数。 |
| `LowLevel1` / `LowLevel2` | 定义第一、第二空头阈值的负向乘数。 |
| `Smoothing` | 用于指标和成交量的移动平均类型：Simple、Exponential、Smoothed、Weighted。 |
| `SmoothingLength` | 移动平均长度。 |
| `SignalBar` | 参与比较的已完成 K 线数量。`1` 表示使用最近一根收盘 K 线。 |
| `VolumeType` | 成交量来源。由于 StockSharp 并不总提供 tick 数，两个选项都会回落到 K 线成交量。 |
| `EnableLongEntries` / `EnableShortEntries` | 允许开多 / 开空。 |
| `EnableLongExits` / `EnableShortExits` | 允许在出现反向信号时平多 / 平空。 |

## 信号与仓位管理

- **开多**：信号 K 线状态变为 `1` 或 `0`，且上一根状态大于 `1`。若当前持有空单且允许平仓，则先平空再开多。
- **开空**：信号 K 线状态变为 `3` 或 `4`，且上一根状态小于 `3`（或 `4`）。若当前持有多单且允许平仓，则先平多再开空。
- **平仓**：当出现相反信号且对应的平仓开关开启时调用 `ClosePosition()`，先平仓再决定是否反向。
- **仓位规模**：使用基础的 `Strategy.Volume` 属性。原顾问中通过两个不同“魔术号”拆分仓位，这里为了简化而合并。

## 实现细节

- 仅处理收盘 K 线，通过 `SubscribeCandles().WhenNew(ProcessCandle)` 订阅数据。
- 自行实现的 DeMarker 保留 DeMax/DeMin 的滚动和，确保与 MetaTrader 版本一致，并在积累到足够的柱数后才输出信号。
- 若成交量缺失，加权指标和阈值都会为零，策略将保持中性状态。
- 原指标中的 JJMA、JurX、ParMA、T3、VIDYA、AMA 等高级平滑方式未实现，可通过 `Smoothing` 选择最接近的类型。
- 状态缓冲区只保留最少的数据（当前、上一根及一个额外位置），以复现 MQL5 `CopyBuffer` 的行为并避免重复触发。

## 使用建议

- 在 Designer 或 Runner 中启动前，先设置好周期和默认仓位，并根据需要运行参数优化。
- 建议联合优化 `DeMarkerPeriod`、`SmoothingLength` 以及各个阈值，它们对入场频率影响很大。
- 策略依赖成交量权重，应使用能提供可靠成交量信息的数据源。
- 如需止损、止盈或资金管理，请结合 StockSharp 的风险控制模块或自定义扩展；本移植版保持了原顾问未提供止损/止盈的特性。
