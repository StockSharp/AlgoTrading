# Expert RSI Stochastic MA 策略

## 概述
**Expert RSI Stochastic MA 策略** 源自 MetaTrader 5 的 `Expert_RSI_Stochastic_MA.mq5` 智能交易系统。本版本使用 StockSharp 的高阶策略框架实现原始逻辑：利用可配置的移动平均线作为趋势过滤器，RSI 指标确认动能，双线随机指标负责触发交易。同时保留原策略的风险控制，包括固定亏损阈值与基于随机指标的追踪止损。

## 指标与参数
所有输入参数与 MQL5 版本一致，并提供默认值，便于在 StockSharp 界面中调参或优化。

| 分类 | 参数 | 默认值 | 说明 |
| --- | --- | --- | --- |
| 通用 | `CandleType` | 15 分钟 | 指标计算使用的 K 线时间框架。 |
| 交易 | `TradeVolume` | `0.01` | 每次开仓的基础数量（手数/合约数）。 |
| RSI | `RsiPeriod` | `3` | RSI 计算周期。 |
| RSI | `RsiPriceType` | 收盘价 | RSI 所使用的价格类型（收盘、开盘、最高、最低、中位、典型、加权）。 |
| RSI | `RsiUpperLevel` | `80` | RSI 超买阈值，用于做空条件。 |
| RSI | `RsiLowerLevel` | `20` | RSI 超卖阈值，用于做多条件。 |
| 随机指标 | `StochKPeriod` | `6` | %K 线周期。 |
| 随机指标 | `StochDPeriod` | `3` | %D 线平滑周期。 |
| 随机指标 | `StochSlowing` | `3` | %K 线的额外平滑参数。 |
| 随机指标 | `StochUpperLevel` | `70` | 双线随机指标的超买水平。 |
| 随机指标 | `StochLowerLevel` | `30` | 双线随机指标的超卖水平。 |
| 移动平均 | `MaMethod` | 简单均线 | 移动平均类型（简单、指数、平滑、加权）。 |
| 移动平均 | `MaPriceType` | 收盘价 | 计算移动平均时使用的价格。 |
| 移动平均 | `MaPeriod` | `150` | 移动平均计算周期。 |
| 移动平均 | `MaShift` | `0` | 取值时向后偏移的已完成 K 线数量。 |
| 风险 | `AllowLossPoints` | `30` | 允许的最大亏损点数（0 表示禁用固定止损）。 |
| 风险 | `TrailingStopPoints` | `30` | 随机指标触发的追踪止损点数（0 表示不用追踪止损，改为极值直接平仓）。 |

> **点值换算**：`AllowLossPoints` 与 `TrailingStopPoints` 会基于 `Security.PriceStep` 转换为绝对价格。若标的价格精度为 3 或 5 位小数，将自动乘以 10 以模拟 MetaTrader 中的“pip”概念。

## 交易逻辑
### 多头条件
1. **趋势过滤**：收盘价需位于移动平均（考虑 `MaShift` 偏移）之上。
2. **动能确认**：RSI 小于 `RsiLowerLevel`。
3. **时机判断**：随机指标的 %K 与 %D 同时低于 `StochLowerLevel`。
4. **持仓检查**：仅在当前没有多头持仓时开多（`Position <= 0`），下单数量为 `TradeVolume` 加上用于对冲可能存在的空头仓位的数量。

### 空头条件
1. **趋势过滤**：收盘价低于移动平均。
2. **动能确认**：RSI 大于 `RsiUpperLevel`。
3. **时机判断**：%K 与 %D 同时高于 `StochUpperLevel`。
4. **持仓检查**：仅在 `Position >= 0` 时开空，必要时自动对冲现有多头。

### 平仓与风控
- **亏损平仓**
  - 当 `AllowLossPoints = 0` 时，策略等待随机指标回到相反的极值区域（多头对应 `StochUpperLevel`，空头对应 `StochLowerLevel`）再平仓。
  - 当 `AllowLossPoints > 0` 时，达到对应点差（换算后的绝对价格差）且随机指标回归中性区域后立即平仓。
- **追踪止损**
  - `TrailingStopPoints > 0`：当交易处于盈利区间并且随机指标触及极值区域时，每根完成 K 线更新追踪止损。多头止损跟随价格下方，空头止损跟随价格上方。
  - `TrailingStopPoints = 0`：不使用追踪止损，盈利单一旦随机指标到达极值立即离场。
- **更新节奏**：追踪止损仅在每根完成 K 线更新一次，与原始 EA 的节奏保持一致。

## 实现说明
- 通过保存最近的移动平均值，实现对 `MaShift` 的偏移取值，等效于 MetaTrader 中的 `shift` 参数。
- RSI 与移动平均均支持多种价格来源。随机指标使用 StockSharp 自带实现（高低价模式），并保留原始的平滑周期设置。
- 所有点数参数均以“point”为单位，内部根据 `PriceStep` 及小数位数自动换算，未设置步长时默认按 1 个价格单位处理。
- 图表输出包括 K 线、移动平均、RSI 与随机指标，便于复盘与视觉验证。
- 根据任务要求，本目录仅提供 C# 版本，不包含 Python 实现。

## 使用建议
- 在非常规报价的品种上部署时，请确认 `Security.PriceStep` 已正确设置，否则点数将按 1 个价格单位换算。
- 如需额外的止损/止盈，可结合 `StartProtection` 或自定义的风险管理模块。
- 建议在优化时同时调整指标周期与风险阈值，三者之间存在明显联动，可获得更平滑的表现。
