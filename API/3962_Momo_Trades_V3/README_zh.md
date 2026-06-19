# Momo Trades V3 策略

## 概览
Momo Trades V3 是将原始 MetaTrader 专家顾问迁移到 StockSharp 框架的动量策略。它保留了 EA 的核心思路：通过多条件 MACD 形态识别配合位移 EMA 滤波，同时增加可选的保本管理以及与原版相似的自动仓位控制模式。

## 交易逻辑
1. **MACD 动量形态**：使用标准参数 `(12, 26, 9)` 和额外的历史位移 `MacdShift`。做多信号包含两种情况：
   - MACD 主线连续上升，第 3 个值等于 0，之后两个值继续上升；
   - MACD 上穿零轴，当前值和之后的值保持为正，而更早的值仍为负。
   做空信号则要求上述条件镜像成立。
2. **EMA 距离过滤**：移位后的收盘价（`MaShift`）必须相对 EMA 至少偏离 `PriceShiftPoints` 个 MetaTrader 点。多头要求价格在 EMA 上方，空头要求在 EMA 下方，以避免在均线附近追单。
3. **单次持仓模式**：仅在当前无仓位时才允许开新仓。持仓期间出现的反向信号被忽略。
4. **日终平仓**：启用 `CloseEndDay` 后，策略会在平台时间 23:00（周五为 21:00）平掉所有仓位，规避隔夜风险。
5. **保本管理**：`UseBreakeven` 打开时，一旦价格运行足够距离，可将止损移动到入场价加 `BreakevenOffsetPoints`，策略即记录该水平；若价格回落（或回升）至该水平，便立即以市价离场。

## 风险控制
- **初始保护**：`StopLossPoints` 与 `TakeProfitPoints` 会通过合约的 `PriceStep` 转换为绝对价差，并传入 `StartProtection`，因此止损/止盈订单会自动附加在仓位上。
- **自动仓位**：若 `UseAutoVolume` 为真，订单数量根据当前资产权益计算。策略取权益的 `RiskFraction`，除以合约名义价值（价格 × 合约手数），再根据交易所的 `VolumeStep` 规范化，并遵守 `VolumeMin`/`VolumeMax` 限制。关闭自动模式时直接使用固定的 `TradeVolume`。

## 指标
- **MACD**：提供主要的动量信号，并结合 `MacdShift` 对历史样本进行评估。
- **EMA**：作为趋势/距离过滤器。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `TimeFrame(15m)` | 生成信号所用的主时间框架。 |
| `MaPeriod` | `int` | `22` | EMA 滤波的周期。 |
| `MaShift` | `int` | `1` | 取样收盘价与 EMA 时所使用的已完成柱数。 |
| `FastPeriod` | `int` | `12` | MACD 的快速 EMA 周期。 |
| `SlowPeriod` | `int` | `26` | MACD 的慢速 EMA 周期。 |
| `SignalPeriod` | `int` | `9` | MACD 信号线 EMA 周期。 |
| `MacdShift` | `int` | `1` | 计算 MACD 形态时的额外位移。 |
| `PriceShiftPoints` | `decimal` | `10` | 移位收盘价与 EMA 的最小距离（MetaTrader 点）。 |
| `TradeVolume` | `decimal` | `0.1` | 未启用自动仓位时的基础手数。 |
| `RiskFraction` | `decimal` | `0.1` | 自动仓位模式下占用的权益比例。 |
| `UseAutoVolume` | `bool` | `false` | 是否启用风险驱动的仓位计算。 |
| `StopLossPoints` | `decimal` | `100` | 初始止损距离（MetaTrader 点），0 表示不设置硬性止损。 |
| `TakeProfitPoints` | `decimal` | `0` | 初始止盈距离，0 表示不设置固定目标。 |
| `CloseEndDay` | `bool` | `true` | 是否在交易日结束时强制平仓。 |
| `UseBreakeven` | `bool` | `false` | 是否启用保本管理。 |
| `BreakevenOffsetPoints` | `decimal` | `0` | 移动至保本价时附加的偏移量。 |

## 使用建议
- 请确保标的提供有效的 `PriceStep`。若缺失数据，策略会退回到 `0.0001` 作为点值换算因子。
- 策略仅在蜡烛结束时处理信号，以保持与原 EA 行为一致。
- 因为同一时间只持有一笔仓位，单笔风险完全由 `TradeVolume`（或自动计算的手数）决定。
