# Forex Fraus M1 策略

## 概述
Forex Fraus M1 策略在 StockSharp 框架中重现 MetaTrader 5 专家顾问“Forex Fraus M1”。该系统属于逆势交易：它在 1 分钟K线上监控周期为 360 的 Williams %R 指标，当指标触及极端值时尝试逆转行情，期望价格快速回归到区间中枢。实现中保留了原始 EA 的风险控制——可选的交易时段过滤、以点数表示的固定止损/止盈以及基于点的移动止损。

## 交易逻辑
- **指标**：周期为 360 的 Williams %R。
- **做多**：当 Williams %R 低于 `-99.9` 时，认为市场极度超卖。如果当前没有多头头寸，则提交市价买单。若 `CloseOppositePositions` 为真，则会在同一笔订单中平掉所有空头仓位。
- **做空**：当 Williams %R 高于 `-0.1` 时，市场极度超买。策略发送市价卖单；若已有多头并启用 `CloseOppositePositions`，则会一并平仓。
- **时间过滤**：开启 `UseTimeControl` 后，仅在 `StartHour`（含）与 `EndHour`（不含）之间评估信号。当 `StartHour > EndHour` 时，交易时段跨越午夜，允许在 `StartHour` 至 23 点及 0 点至 `EndHour - 1` 之间交易。

## 风险管理
- **止损**：根据 `StopLossPips * PipSize` 计算。多头在入场价下方设定，空头在入场价上方设定。当完成蜡烛的最低价触及该水平时，立刻市价平仓。
- **止盈**：根据 `TakeProfitPips * PipSize` 计算。多头在入场价上方，空头在入场价下方；当最高价或最低价达到该水平时锁定利润。
- **移动止损**：若 `TrailingStopPips` 和 `TrailingStepPips` 都大于零，当价格至少向有利方向运行 `TrailingStopPips + TrailingStepPips` 点时调整止损。多头止损跟随收盘价下方 `TrailingStopPips` 点，空头止损跟随收盘价上方 `TrailingStopPips` 点。
- **点值**：`PipSize` 定义 1 个点对应的价格增量。五位报价的外汇品种通常设为 `0.0001`，日元三位报价可设为 `0.01` 等。

策略使用完成蜡烛的最高/最低来检测止损和止盈。如果同一根蜡烛同时触及两个水平，则优先视为止损触发，保持原 EA 的保守行为。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | `0.1` | 新仓位的交易量。 |
| `StopLossPips` | `50` | 距离入场价的止损点数，设为 0 可禁用。 |
| `TakeProfitPips` | `150` | 距离入场价的止盈点数，设为 0 可禁用。 |
| `TrailingStopPips` | `1` | 移动止损的基础距离，设为 0 表示不使用。 |
| `TrailingStepPips` | `1` | 每次移动止损所需的额外盈利点数。 |
| `UseTimeControl` | `true` | 是否启用交易时段过滤。 |
| `StartHour` | `7` | 交易开始小时（0-23）。 |
| `EndHour` | `17` | 交易结束小时（1-24，不包含）。 |
| `CloseOppositePositions` | `true` | 开仓前是否在同一笔订单中反向平仓。 |
| `WilliamsPeriod` | `360` | Williams %R 指标的计算周期。 |
| `CandleType` | `1 minute` | 用于计算的蜡烛类型。 |
| `PipSize` | `0.0001` | 一个点对应的价格增量。 |

## 其他说明
- 策略使用 StockSharp 的高级蜡烛订阅与指标绑定，无需手动维护历史缓存。
- 止损、止盈以及移动止损的判断均基于已完成的蜡烛，避免对未收盘数据做出决策。
- 按项目规范在启动时调用一次 `StartProtection()`，具体风险控制逻辑在策略内部实现。
- 请根据具体交易品种调整 `PipSize`，确保点数与价格之间的换算准确。
