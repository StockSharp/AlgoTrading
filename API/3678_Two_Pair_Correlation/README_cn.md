# 双品种相关性策略

## 概述

**双品种相关性策略** 将 MetaTrader 专家顾问 *“2-Pair Correlation EA”*（目录 `MQL/52043`）移植到 StockSharp 高级 API。策略同时监听两只高度相关的加密资产（主腿为 BTCUSD，对冲腿为 ETHUSD）的买价，一旦价差突破阈值，便构建市场中性组合。

### 核心流程

1. **风险闸门**：持续跟踪投资组合权益。一旦权益从历史峰值回撤超过 `MaxDrawdownPercent`，策略暂停开仓，直到权益恢复到峰值的 `RecoveryPercent` 以上。
2. **波动过滤**：两只标的的 5 分钟 K 线被送入 `AtrPeriod` 长度的 `AverageTrueRange` 指标。若任一 ATR 超过 `PriceDifferenceThreshold * 0.01`，则视为波动过大，本轮信号被跳过。
3. **价差检测**：订阅两只标的的 Level1 数据并在每次更新时评估买价差。当 `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold` 时，做多 BTCUSD、做空 ETHUSD；当价差跌破 `-PriceDifferenceThreshold` 时执行反向操作。
4. **动态手数**：下单量来自账户权益的 `RiskPercent`，再除以合成止损距离 `StopLossPips * PriceStep`。结果会依据交易所的数量步长/上下限进行归一化，与原始 EA 的“动态手数”一致。
5. **篮子止盈**：实时计算两条腿的总浮盈（以账户货币计价）。当达到 `MinimumTotalProfit` 时，无论方向如何都立即平掉整组持仓。

## 所需行情

- **Level1**（最优买卖价）：主标的 `Security` 与对冲标的 `SecondSecurity` 均需提供。
- **K 线**：两只标的的 `AtrCandleType`（默认 5 分钟）用于计算 ATR。

请确保证券对象提供合理的 `PriceStep`、`StepPrice`、`VolumeStep` 以及数量上下限，以便手数换算与盈亏折算准确还原 MQL 行为。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `SecondSecurity` | `Security` | — | 对冲腿（原始 EA 中为 ETHUSD）。 |
| `MaxDrawdownPercent` | `decimal` | `20` | 超过该回撤后暂停开仓。 |
| `RiskPercent` | `decimal` | `2` | 每次交易占用的权益百分比。 |
| `PriceDifferenceThreshold` | `decimal` | `100` | 触发进场的买价差阈值。 |
| `MinimumTotalProfit` | `decimal` | `0.30` | 触发篮子平仓的总浮盈（账户货币）。 |
| `AtrPeriod` | `int` | `14` | ATR 波动过滤的周期。 |
| `RecoveryPercent` | `decimal` | `95` | 回撤后恢复到该百分比才重新开仓。 |
| `StopLossPips` | `int` | `50` | 将 `RiskPercent` 转换为手数的合成止损距离。 |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | 用于 ATR 的 K 线类型。 |

## 文件

- `CS/TwoPairCorrelationStrategy.cs` — 策略实现。
- `README.md` — 英文说明。
- `README_cn.md` — 中文说明。
- `README_ru.md` — 俄文说明。
