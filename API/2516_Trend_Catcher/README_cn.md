# 趋势捕捉策略

## 概述
趋势捕捉（Trend Catcher）策略来源于 MetaTrader 5 专家顾问 “Trend_Catcher_v2”的移植版本。策略将三条指数移动平均线与 Parabolic SAR 指标结合在一起，用于识别趋势反转和趋势延续信号。算法基于单一品种与单一周期，并且只在 K 线收盘后做出决策，因此既适合在 StockSharp Designer 中回测，也能通过 StockSharp API 应用在实时环境中运行。

## 指标与过滤条件
- **Parabolic SAR**：检测价格相对于 SAR 的翻转，提示潜在的趋势反向点。
- **慢速 EMA**：衡量大周期的主方向。
- **快速 EMA**：跟随价格的短期变化，用于确认当前动能。
- **触发 EMA**：确保入场价格不过度偏离均线，避免追高或杀跌。
- **交易日开关**：可按星期选择允许或禁止交易日。

## 交易规则
### 做多条件
1. 收盘价位于当前 Parabolic SAR 之上；
2. 前一根 K 线的收盘价低于前一周期的 Parabolic SAR（多头翻转）；
3. 快速 EMA 高于慢速 EMA，表明处于上升趋势；
4. 收盘价高于触发 EMA，过滤逆势信号；
5. 当前没有持仓，且本根 K 线内没有关闭过仓位。

### 做空条件
满足上述条件的镜像：
1. 收盘价低于当前 Parabolic SAR；
2. 前一根 K 线收盘价高于前一周期 Parabolic SAR（空头翻转）；
3. 快速 EMA 低于慢速 EMA；
4. 收盘价低于触发 EMA；
5. 当前没有持仓，且本根 K 线内没有关闭过仓位。

当启用 **Reverse Signals**（信号反转）参数时，做多与做空条件会互换，从而以相反方向执行突破信号。

## 仓位管理
- **自动止损**：若启用，止损距离等于价格与 Parabolic SAR 的差值乘以 `StopLossCoefficient`，并限制在 `MinStopLoss` 与 `MaxStopLoss` 范围内。
- **自动止盈**：止盈距离等于止损乘以 `TakeProfitCoefficient`。停用自动模式时使用 `ManualStopLoss` 与 `ManualTakeProfit` 的固定数值。
- **风险化仓位**：根据账户权益和 `RiskPercent` 计算下单数量。如果上一笔交易亏损且启用 **Use Martingale**，下单量会乘以 `MartingaleMultiplier`。
- **保本与追踪止损**：当浮盈达到 `BreakevenTrigger` 时，将止损移动至入场价加上 `BreakevenOffset`（做空则减去）。当收益继续扩大到 `TrailingTrigger` 时，止损会以 `TrailingStep` 的距离跟随价格移动。
- **反向信号平仓**：启用 `CloseOnOppositeSignal` 时，一旦出现反向信号会立即平掉已有仓位。
- **单根 K 线仅一次入场**：策略记录最近的平仓时间，在同一根 K 线内不会再次开仓。

## 参数列表
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于计算的主要 K 线周期。 | 15 分钟 |
| `CloseOnOppositeSignal` | 出现反向信号时立即平仓。 | `true` |
| `ReverseSignals` | 交换做多与做空的条件。 | `false` |
| `TradeMonday` … `TradeFriday` | 控制周一到周五是否允许交易。 | `true` |
| `SlowMaPeriod` | 慢速 EMA 的周期。 | `200` |
| `FastMaPeriod` | 快速 EMA 的周期。 | `50` |
| `FastFilterPeriod` | 触发 EMA 的周期。 | `25` |
| `SarStep` | Parabolic SAR 的加速步长。 | `0.004` |
| `SarMax` | Parabolic SAR 的最大加速。 | `0.2` |
| `AutoStopLoss` | 是否启用自动止损计算。 | `true` |
| `AutoTakeProfit` | 是否启用自动止盈计算。 | `true` |
| `MinStopLoss` / `MaxStopLoss` | 止损距离的上下限。 | `0.001` / `0.2` |
| `StopLossCoefficient` | 计算止损时的倍数。 | `1` |
| `TakeProfitCoefficient` | 计算止盈时的倍数。 | `1` |
| `ManualStopLoss` | 关闭自动模式时使用的固定止损。 | `0.002` |
| `ManualTakeProfit` | 关闭自动模式时使用的固定止盈。 | `0.02` |
| `RiskPercent` | 单笔交易承担的权益百分比。 | `2` |
| `UseMartingale` | 亏损后放大下次下单量。 | `true` |
| `MartingaleMultiplier` | 马丁倍率。 | `2` |
| `BreakevenTrigger` | 触发保本的收益阈值。 | `0.005` |
| `BreakevenOffset` | 移动到保本时的缓冲距离。 | `0.0001` |
| `TrailingTrigger` | 开始追踪止损的收益阈值。 | `0.005` |
| `TrailingStep` | 追踪止损与价格的固定距离。 | `0.001` |

## 使用提示
- 策略始终使用市价单入场和出场，若需要限制滑点应在交易适配器中配置。
- 由于所有计算基于收盘价，回测精度依赖于提供的 K 线粒度与历史数据质量。
- 全部参数通过 `StrategyParam` 暴露，可在 StockSharp Designer 中进行优化或在自动化流程中动态调整。
