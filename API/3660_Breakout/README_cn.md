# Breakout Strategy

## 概述

Breakout Strategy 是将 MetaTrader 5 专家顾问 `BreakoutStrategy.mq5` 移植到 StockSharp 的结果。策略跟踪一段时间内的最高价和最低价，当价格突破通道边界时入场，并通过第二组唐奇安通道对持仓进行跟踪止损，完全对应原始 EA 的逻辑。

## 交易逻辑

1. **入场通道**：`EntryPeriod` 内的最高价和最低价通过 `EntryShift` 进行延迟，以避免在计算突破时使用当前 K 线。
2. **突破检测**：如果当前 K 线最高价突破上轨并超过一个最小报价单位，则触发做多；若最低价跌破下轨并减去一个最小报价单位，则触发做空。
3. **出场通道**：`ExitPeriod` 内的最高价和最低价同样经过 `ExitShift` 延迟。启用 `UseMiddleLine` 时，策略会在外轨和中轨之间选择（多头取较大值，空头取较小值）作为跟踪止损水平。
4. **仓位管理**：当多头仓位的最低价跌破跟踪水平时离场；空头仓位的最高价触及跟踪水平时平仓。出现反向信号时，策略会先平掉现有仓位再按新方向建仓。
5. **风险控制**：仓位规模取决于 `RiskPerTrade`。策略读取账户权益，结合合约的 `PriceStep` 与 `StepPrice` 将止损距离转换为金额，并根据 `VolumeStep`、`VolumeMin`、`VolumeMax` 调整下单手数，使潜在亏损接近设定的资金百分比。

## 参数

| 名称 | 说明 |
| --- | --- |
| `CandleType` | 使用的 K 线类型，默认是 1 小时。 |
| `EntryPeriod` | 入场通道的回溯长度。 |
| `EntryShift` | 入场通道的延迟棒数，`1` 对应原始 EA 设置。 |
| `ExitPeriod` | 跟踪通道的回溯长度。 |
| `ExitShift` | 跟踪通道的延迟棒数。 |
| `UseMiddleLine` | 是否在计算跟踪止损时使用唐奇安中轨。 |
| `RiskPerTrade` | 单笔交易允许的资金风险比例。 |

## 说明

- C# 源码中的注释全部使用英文，满足仓库要求。
- 策略依赖 StockSharp 的高级 API：K 线订阅、`Highest`/`Lowest` 指标以及 `Shift` 指标实现数据延迟，不再手动管理数组。
- 项目未附带自动化测试，请在真实交易前先在本地环境验证策略表现。
