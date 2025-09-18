# Kloss Simple Strategy
[English](README.md) | [Русский](README_ru.md)

**Kloss Simple Strategy** 是将 MetaTrader 4 指标顾问 `Kloss_.mq4` 迁移到 StockSharp 平台的版本。策略完全保留原有思路：使用基于加权收盘价的指数移动平均线（EMA）、商品通道指数（CCI）以及随机指标（Stochastic）。所有信号均依据上一根完成的 K 线计算，从而复现 MQL 代码中的一根柱偏移逻辑。仓位规模既可以固定，也可以按照账户权益的百分比动态调整，对应原策略的“Lots == 0”机制。

## 核心思想

1. 通过 **CCI** 与 **Stochastic** 的阈值监控市场动能。
2. 使用加权收盘价的短期 **EMA** 进行趋势确认。
3. 仅在上一根已完成的 K 线满足全部条件时才开仓，避免使用未完成数据。
4. 允许在同一方向上开多笔仓位，并受 `MaxOrders` 限制，与 MT4 版本保持一致。

## 指标设置

- **EMA (MaPeriod)**：采用加权收盘价 `(Close * 2 + High + Low) / 4)`，与 MetaTrader 的 `PRICE_WEIGHTED` 模式一致，用于过滤短期趋势。
- **CCI (CciPeriod)**：衡量价格偏离均值的程度，`±CciLevel` 控制信号强度。
- **Stochastic (StochasticKPeriod / DPeriod / Smooth)**：关注 %K 主线相对于 50 的偏离程度，`StochasticLevel` 定义超买/超卖阈值。

所有指标都基于参数 `CandleType` 指定的主时间框架，并且只在 K 线收盘后更新，从而保证回测与实时表现的一致性。

## 交易逻辑

### 做多条件

1. 上一根 K 线的收盘价高于上一周期的 EMA。
2. 上一周期的 CCI 小于 `-CciLevel`，说明动能过度下行。
3. 上一周期的 Stochastic %K 小于 `50 - StochasticLevel`，确认超卖状态。
4. 满足条件时，先平掉空头，再在不超过 `MaxOrders` 限制的前提下加多头仓位。

### 做空条件

1. 上一根 K 线的收盘价低于上一周期的 EMA。
2. 上一周期的 CCI 大于 `+CciLevel`，说明动能过度上行。
3. 上一周期的 Stochastic %K 大于 `50 + StochasticLevel`，确认超买状态。
4. 满足条件时，先平掉多头，再按 `MaxOrders` 限制增加空头仓位。

### 离场管理

- **止损 / 止盈**：以点数设置，若数值大于零则启用 StockSharp 的内置保护模块。
- **反向信号**：当出现反向条件时会先平仓，再尝试反向建仓，与原始 EA 的执行流程一致。

## 仓位控制

- **OrderVolume**：默认固定手数，对应 MT4 中的 `Lots` 参数。
- **RiskPercentage**：大于零时按账户权益百分比计算下单量，优先使用合约保证金数据，否则退回到价格估算，复刻 `Lots == 0` 的动态仓位算法。
- **MaxOrders**：限制同向持仓的累计手数，最大为 `MaxOrders * OrderVolume`。

## 参数说明

| 参数 | 说明 |
|------|------|
| `OrderVolume` | 固定下单手数，当 `RiskPercentage` 为零时生效。 |
| `MaPeriod` | EMA 的周期长度。 |
| `CciPeriod` | 计算 CCI 的样本数量。 |
| `CciLevel` | 触发信号的 CCI 阈值。 |
| `StochasticKPeriod` | 随机指标 %K 的计算周期。 |
| `StochasticDPeriod` | %D 平滑周期。 |
| `StochasticSmooth` | 对 %K 的额外平滑。 |
| `StochasticLevel` | 相对于 50 的偏移阈值。 |
| `MaxOrders` | 同方向最多允许的开仓次数。 |
| `StopLossPoints` | 止损距离（点）。 |
| `TakeProfitPoints` | 止盈距离（点）。 |
| `RiskPercentage` | 动态仓位所占账户权益百分比。 |
| `CandleType` | 指定用于计算的 K 线类型。 |

## 实战提示

- 适用于波动频繁的短周期市场，可快速捕捉动量回归。
- 加权收盘价让 EMA 兼顾价格范围与收盘价，响应更灵敏。
- 所有判断都基于上一根完成的 K 线，避免指标重绘，保证回测一致性。
- 设置 `OrderVolume` 与 `MaxOrders` 时需考虑交易品种的合约大小和最小变动单位，确保指令能够实际成交。
