# RM Stochastic Band 策略

## 概述

**RM Stochastic Band Strategy** 是 MetaTrader 专家顾问 *EA RM Stochastic Band*（作者 Ronny Maheza）的 StockSharp 高层 API 版本。策略同时监控三个不同时框上的随机指标，并且仅当三个时框的 %K 值同时指示超买或超卖时才开仓。进场之后，使用位于最高时框上的平均真实波幅（ATR）来计算止损和止盈，完全再现原始 EA 的波动率管理方法。此外，实现了可配置的最小资金阈值和自适应的点差过滤器。

## 核心逻辑

1. **多时框确认**  
   - 基础时框（默认 1 分钟）产生主要信号。  
   - 中级和高级时框（默认 5 分钟、15 分钟）必须与基础信号方向一致。  
   - 仅当三个时框的 %K 同时低于超卖阈值时买入；当三个时框的 %K 同时高于超买阈值时卖出。

2. **ATR 波动率止损/止盈**  
   - ATR 在最高时框（默认 15 分钟）上计算。  
   - 止损 = `入场价 ± ATR * StopLossMultiplier`。  
   - 止盈 = `入场价 ± ATR * TakeProfitMultiplier`。  
   - 在基础时框的已完成蜡烛上检查价格触及情况并市价离场。

3. **执行与风控过滤**  
   - 根据 Level-1 的最佳买卖价估算点差；如果当前点差超过标准上限，则使用更宽松的“分型账户”上限，与原 EA 的逻辑一致。  
   - 当投资组合价值低于 `MinMargin` 时暂停交易。  
   - 同一时间仅允许一笔持仓，且存在活动委托时不会开新仓。

## 指标与订阅

| 指标 | 时框 | 用途 |
|------|------|------|
| 随机指标 (Stochastic Oscillator) | 基础时框 (默认 1 分钟) | 产生主要信号，仅使用 %K。 |
| 随机指标 | 中级时框 (默认 5 分钟) | 确认信号方向。 |
| 随机指标 | 高级时框 (默认 15 分钟) | 长周期确认。 |
| 平均真实波幅 (ATR) | 高级时框 (默认 15 分钟) | 计算止损和止盈距离。 |

策略还订阅 Level-1 行情以获取最佳买卖价，确保点差过滤器能够工作。

## 入场规则

- **做多**：三个时框的 %K 值均低于 `OversoldLevel`。策略以 `OrderVolume` 的数量市价买入，并记录 ATR 计算出的止损/止盈。  
- **做空**：三个时框的 %K 值均高于 `OverboughtLevel`。策略以相同数量市价卖出。

## 出场规则

- **止损**：多单在价格低点触及 `入场价 - ATR * StopLossMultiplier` 时平仓；空单在高点触及 `入场价 + ATR * StopLossMultiplier` 时平仓。  
- **止盈**：多单在高点触及 `入场价 + ATR * TakeProfitMultiplier` 时平仓；空单在低点触及 `入场价 - ATR * TakeProfitMultiplier` 时平仓。  
- 每次平仓后都会清空内部的止损/止盈缓存，等待下一次信号重新计算。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次市价订单的成交量。 | 0.1 |
| `StochasticLength` | %K 的回溯长度。 | 5 |
| `StochasticSmoothing` | %K 的平滑参数。 | 3 |
| `StochasticSignalLength` | %D 的长度。 | 3 |
| `AtrPeriod` | 在高时框计算 ATR 的周期。 | 14 |
| `StopLossMultiplier` | ATR 止损倍数。 | 1.5 |
| `TakeProfitMultiplier` | ATR 止盈倍数。 | 3.0 |
| `MinMargin` | 允许交易的最小投资组合价值。 | 100 |
| `MaxSpreadStandard` | 标准账户允许的最大点差。 | 3 |
| `MaxSpreadCent` | 当标准上限被突破时使用的备用点差上限。 | 10 |
| `OversoldLevel` | 判定超卖的 %K 阈值。 | 20 |
| `OverboughtLevel` | 判定超买的 %K 阈值。 | 80 |
| `BaseCandleType` | 基础时框（默认 1 分钟 K 线）。 | 1 分钟 |
| `MidCandleType` | 中级确认时框。 | 5 分钟 |
| `HighCandleType` | 高级确认 + ATR 时框。 | 15 分钟 |

所有参数均支持与原始 EA 相同的优化范围。

## 实现细节

- 指标值通过 `SubscribeCandles(...).BindEx(...)` 获取，完全遵循 AGENTS.md 中的高层 API 要求，未直接访问内部缓存。  
- 点差依据 Level-1 数据实时计算；若行情源缺少买卖报价，策略将保持待机状态，避免在不可靠的市场条件下下单。  
- 头寸管理完全采用市价单，与原 EA 的做法保持一致。  
- 原 MQL 代码虽定义了保本、追踪等输入参数，但并未实现相关逻辑，因此移植版本也不包含这些功能。

## 使用建议

1. 在接入策略之前确认数据源提供 Level-1 行情，否则点差过滤会阻止交易。  
2. 根据标的资产的波动率调整随机指标阈值与 ATR 倍数。  
3. 在回测或优化时可尝试不同的时框组合，以适配与原始 M1/M5/M15 结构不同的市场周期。
