# BSS Triple EMA Separation 策略

## 概述

**BSS Triple EMA Separation 策略** 是 MetaTrader 5 专家顾问 “BSS 1_0”（MQL ID 20591）的 StockSharp 版本。算法同时监控三个周期逐渐递增的移动平均线，并要求它们之间至少保持指定的最小间距。一旦满足条件，策略便顺势开仓，并对连续入场之间的间隔和最大仓位规模进行控制。

本实现保持了原始 EA 的核心逻辑，并通过 `StrategyParam` 对象公开所有关键参数。按照要求，代码注释和主要文档均使用英文撰写。

## 交易逻辑

1. 根据 `CandleType` 参数订阅单一时间框架的 K 线数据，并计算三条移动平均线（快速、中速、慢速）。每条平均线都可以选择不同的平滑方式（简单、指数、平滑或线性加权）。
2. **做多条件**（在收盘完毕的 K 线上检查）：
   - `慢速 MA - 中速 MA >= MinimumDistance`。
   - `中速 MA - 快速 MA >= MinimumDistance`。
3. **做空条件** 与上述相反：
   - `快速 MA - 中速 MA >= MinimumDistance`。
   - `中速 MA - 慢速 MA >= MinimumDistance`。
4. 在发送委托之前需要满足以下前置条件：
   - 所有指标均已形成，策略处于允许交易的状态（`IsFormedAndOnlineAndAllowTrading`）。
   - 自上一次入场以来已超过 `MinimumPauseSeconds` 指定的秒数。
   - 新增仓位不会突破 `MaxPositions` 限定的最大净持仓。
5. 当出现反向信号时，会先平掉相反方向的仓位，随后在下一根满足条件的 K 线上考虑开仓，这一点与原始 MQL 策略的行为保持一致。
6. 每当产生新的入场或加仓，策略都会记录成交时间，以确保后续交易遵守冷却时间。

该策略不包含固定止损或止盈，风险控制主要依靠距离过滤、交易间隔和最大仓位限制。

## 参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | 0.1 | 单笔下单量。策略的净头寸被限制在 `OrderVolume * MaxPositions` 之内。 |
| `MaxPositions` | 2 | 同方向可同时持有的最大总量（以手数/批数计）。 |
| `MinimumDistance` | 0.0005 | 相邻两条均线之间所需的最小价格差。请根据标的设置合适的值（例如 5 位报价的 EURUSD 中 0.0005 等于 5 个点）。 |
| `MinimumPauseSeconds` | 600 | 新增仓位之间的最小等待时间（秒）。平仓不会重置计时器，只有新的入场会更新。 |
| `FirstMaPeriod` | 5 | 快速移动平均线的周期，必须小于 `SecondMaPeriod`。 |
| `FirstMaMethod` | Exponential | 快速均线的平滑方式（Simple、Exponential、Smoothed、LinearWeighted）。 |
| `SecondMaPeriod` | 25 | 中速移动平均线的周期，必须小于 `ThirdMaPeriod`。 |
| `SecondMaMethod` | Exponential | 中速均线的平滑方式。 |
| `ThirdMaPeriod` | 125 | 慢速移动平均线的周期。 |
| `ThirdMaMethod` | Exponential | 慢速均线的平滑方式。 |
| `CandleType` | 1 分钟 | 用于计算指标和判断信号的 K 线类型。 |

## 实现细节

- 使用 StockSharp 的高层 API：`SubscribeCandles` 负责订阅数据流，`Bind` 将数据同时传递给三个均线指标及信号处理函数。
- 移动平均线在 `OnStarted` 中根据参数选择相应的实现，默认配置（三条指数均线、取收盘价）与原始 EA 完全一致。
- 调用 `StartProtection()` 激活 StockSharp 的内置仓位保护机制。
- 重写 `OnPositionChanged`，在净头寸增加时记录成交时间，从而实现与 MQL 策略相同的冷却逻辑。
- 在开新仓前会先关闭反向仓位，确保净头寸不会在同一时刻直接从多头转为空头或反之。

## 使用建议

1. 根据交易品种的最小报价单位调整 `MinimumDistance`：
   - EURUSD（5 位报价）：`0.0005` ≈ 5 点。
   - USDJPY（3 位报价）：`0.05` ≈ 5 点。
2. 结合不同时间框架和市场状态调整三条均线的周期及平滑方式。
3. 在较慢的时间框架上可以适当增加 `MinimumPauseSeconds`，以避免过度交易；在较快时间框架上则可以适度缩短。
4. 与 `OrderVolume` 联合调整 `MaxPositions`，确保实际头寸规模符合资金管理计划。

## 与原版 EA 的差异

- 原 EA 支持选择不同的价格类型（开盘价、高价、低价等）。当前移植版本仅使用收盘价，这与默认配置一致。
- 策略按照净头寸模型工作：多头持仓为正，空头持仓为负。当净头寸达到 `MaxPositions` 限制时，将不会继续加仓，直到仓位被部分或全部平掉，这与原始 MQL 实现的仓位计数逻辑一致。

通过上述配置，您可以在 StockSharp 生态中复现 BSS 策略的主要思想，并根据需要进一步叠加风险控制或其他分析模块。
