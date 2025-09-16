# Exp Hans Indicator Cloud System Tm Plus 策略

## 概述
Exp Hans Indicator Cloud System Tm Plus 是一套基于交易时段的突破策略，用于复刻原始 MQL5 智能交易系统。算法在指定周期的 K 线上跟踪 Hans 指标的颜色状态：当出现多头颜色（0/1）后又恢复到通道内部时开多仓；当出现空头颜色（3/4）后又回到通道内部时开空仓。所有决策都基于已完成的 K 线，并保留了 MQL 版本中的点数止损/止盈和持仓时间限制。

策略只处理 `GetWorkingSecurities()` 返回的单一品种与蜡烛序列。订单数量由策略的 `Volume` 属性与资金管理系数共同决定。

## 指标逻辑
1. 将 K 线时间从经纪商时区（`LocalTimeZone`）转换到目标时区（`DestinationTimeZone`）。默认设置为 GMT+4，与原指标保持一致。
2. 每个交易日采集两个伦敦时段区间：
   - **区间 1**：目的地时间 04:00–08:00。其高/低点构成初始突破通道。
   - **区间 2**：目的地时间 08:00–12:00。完成后会替换区间 1，供当日余下时间使用。
3. 两个区间都会向上/向下各扩展 `PipsForEntry` 个点。点值根据标的的 `PriceStep` 计算，若小数位为 3 或 5，则等同于 MetaTrader 的 10 倍 point。
4. K 线颜色与原指标完全一致：
   - 收盘价高于上轨 → 颜色 `0`（阳线）或 `1`（阴线）。
   - 收盘价低于下轨 → 颜色 `4`（阴线）或 `3`（阳线）。
   - 收盘价位于通道内部 → 中性颜色 `2`。

## 交易规则
- **入场**：上一根已完成的 K 线为多头颜色（0/1），而最新一根不再是多头颜色时（且允许做多），在下一根 K 线开盘执行市价买入。若上一根为空头颜色（3/4）并随后恢复，则在允许做空的情况下开空仓。
- **离场**：
  - 当上一根颜色反向（做多遇到 3/4，做空遇到 0/1）时平仓。
  - 启用 `UseTimeExit` 时，持仓时间超过 `HoldingMinutes` 自动平仓。
  - `StopLossPoints` 与 `TakeProfitPoints` 提供点数止损/止盈。若标的缺少有效的 `PriceStep`，对应功能会被跳过。
- 平仓始终优先于开仓，确保翻仓时先清掉已有头寸。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `MoneyManagement` | 每次下单使用的 `Volume` 比例，≤ 0 时退回到完整 `Volume`。 | `0.1` |
| `MoneyMode` | 与原脚本一致的资金管理模式占位符，目前仅实现 `Lot`。 | `Lot` |
| `StopLossPoints` / `TakeProfitPoints` | 以点数表示的止损/止盈，设置为 `0` 可关闭。 | `1000` / `2000` |
| `DeviationPoints` | 可接受的最大滑点（点）。仅保留配置项，StockSharp 的市价单无法强制该限制。 | `10` |
| `AllowBuyEntries` / `AllowSellEntries` | 是否允许做多/做空入场。 | `true` |
| `AllowBuyExits` / `AllowSellExits` | 是否允许对多/空头自动平仓。 | `true` |
| `UseTimeExit` | 启用时间止损。 | `true` |
| `HoldingMinutes` | 最大持仓时间（分钟）。 | `1500` |
| `PipsForEntry` | 区间上下扩展的点数。 | `100` |
| `SignalBar` | 信号所使用的已完成 K 线偏移。建议 ≥ 1 与 MT5 行为保持一致。 | `1` |
| `LocalTimeZone` | 经纪商服务器时区（小时）。 | `0` |
| `DestinationTimeZone` | 指标使用的目标时区。 | `4` |
| `CandleType` | 计算 Hans 指标的蜡烛类型。 | 30 分钟线 |

## 资金管理与执行
- 订单数量 = `Volume * MoneyManagement`，并按 `VolumeStep` 四舍五入。若结果 ≤ 0，则使用一个 `VolumeStep` 的最小量。
- 反向信号出现时，会一次性发送包含新仓位量和旧仓位反向量的市价单，等效于原 MQL 脚本中的 `BuyPositionOpen`/`SellPositionOpen`。
- 每次入场都会重新计算止损/止盈，并在平仓后清除。

## 使用建议
1. 选择带有有效 `PriceStep`、`Decimals` 和 `VolumeStep` 元数据的交易品种。
2. 在启动前设置策略 `Volume`，资金管理系数会基于该值计算下单量。
3. 选择与 MT5 一致的周期（默认 M30），所有判断均基于收盘 K 线。
4. 如数据源时区与默认 GMT+4 不同，请调整 `LocalTimeZone`/`DestinationTimeZone` 以匹配原始指标。
5. 关注日志信息：若缺少点值数据，风险控制会被禁用。

## 实现细节
- 使用 `SubscribeCandles` 高级接口处理完成 K 线，无需手动维护指标缓冲区。
- 每根 K 线结束时重新计算有效区间，仅保留当日最新结果，不创建额外历史集合。
- `DeviationPoints` 仅作兼容保留，StockSharp 市价订单无法限制滑点。
- 在 `OnReseted()` 中重置内部状态，便于重复回测。

## 限制
- 当前实现仅支持 `SignalBar ≥ 1`。若需要 `0`，需在高频 Tick 数据下实现额外逻辑。
- `MoneyMode` 中除 `Lot` 以外的模式尚未实现，如需使用请自行扩展 `GetOrderVolume()`。
- 若标的缺少有效的 `PriceStep`，所有基于点的距离（止损、止盈、区间扩展）都会被跳过。

