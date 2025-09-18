# Russian20 Time Filter Momentum 策略

## 概述
**Russian20 Time Filter Momentum 策略** 源自 MetaTrader 4 专家顾问 `Russian20-hp1.mq4`，原作者为 Gordago Software Corp. 该算法在 30 分钟 K 线图上结合 20 周期简单移动平均线（SMA）与 5 周期 Momentum 指标，只在趋势与动量方向一致时入场，并可选地限制在指定交易时段内执行。

## 交易逻辑
- **数据周期：** 默认使用 30 分钟 K 线（对应 MT4 的 `PERIOD_M30`），所有信号均在蜡烛收盘后评估，以保持与原脚本相同的“收盘触发”行为。
- **指标：**
  - 可配置周期的简单移动平均线（默认 20）。
  - 可配置周期的 Momentum 指标（默认 5），中性水平设为 100，与 MetaTrader 输出一致。
- **多头入场条件：**
  1. 收盘价高于 SMA。
  2. Momentum 高于阈值（默认 100）。
  3. 当前收盘价高于上一根 K 线的收盘价。
- **空头入场条件：**
  1. 收盘价低于 SMA。
  2. Momentum 低于阈值。
  3. 当前收盘价低于上一根收盘价。
- **离场规则：**
  - 多头：Momentum 回落至阈值以下或达到盈利目标时平仓。
  - 空头：Momentum 升至阈值以上或达到盈利目标时平仓。

## 交易时段过滤
原始 MQL4 程序提供了可选的交易时间窗口（默认 14:00–16:00）。移植版本通过 `UseTimeFilter`、`StartHour`、`EndHour` 参数还原同样的行为。启用过滤后，策略会在时段之外跳过入场与出场逻辑，完全复制原脚本的早退处理方式。

## 风险控制
MQL4 版本为每笔交易附加固定 20 点的盈利目标。本策略同样以“点”（pip）为单位设置距离，并根据合约的 `PriceStep` 自动调整，以兼容 3 位或 5 位小数报价。将 `TakeProfitPips` 设为 0 可关闭盈利目标。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 30 分钟 K 线 | 用于计算信号的数据类型。 |
| `MovingAverageLength` | 20 | SMA 周期。 |
| `MomentumPeriod` | 5 | Momentum 周期。 |
| `MomentumThreshold` | 100 | Momentum 中性阈值，用于入场与离场判断。 |
| `TakeProfitPips` | 20 | 盈利目标距离（点），0 表示禁用。 |
| `UseTimeFilter` | false | 是否启用时段过滤。 |
| `StartHour` | 14 | 允许交易的起始小时（含，0–23）。 |
| `EndHour` | 16 | 允许交易的结束小时（含，0–23）。 |

所有参数都通过 `StrategyParam<T>` 定义，可直接在 StockSharp 界面中查看并用于优化。

## 实现细节
- 使用高层 API `SubscribeCandles().Bind(...)`，指标值直接传入处理函数，无需手动维护历史序列。
- 仅缓存上一根收盘价，用于比较连续蜡烛，符合仓库对性能与内存的要求。
- 根据 `Security.PriceStep` 自动推导“点”大小，确保盈利目标在 4/5 位小数报价的外汇品种上依然准确。
- 若主机环境支持，可通过 `DrawCandles`、`DrawIndicator`、`DrawOwnTrades` 等函数快速在图表上可视化策略行为。

## 使用建议
- 根据交易品种调整蜡烛类型；对于多数外汇货币对，默认的 30 分钟周期与原策略一致。
- 启用时段过滤时请确保 `StartHour` 小于或等于 `EndHour`，否则由于原脚本的实现方式，策略将在全天被动保持空闲。
- 原始版本没有设置止损，真实交易时建议结合 StockSharp 的保护机制或外部风控方案。
