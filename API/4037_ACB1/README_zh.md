# ACB1 策略

## 概述

**ACB1 策略** 是 MetaTrader 专家顾问 `MQL/8586/ACB1.MQ4` 的 StockSharp 版本。原始脚本针对 EURUSD，在日线级别出现强势突破时入场。本移植保留核心思想，并采用 StockSharp 的高阶 API：

- 日线 (`SignalCandleType`) 用于判定突破方向，并提供止损/止盈基准。
- 4 小时线 (`TrailCandleType`) 提供 `(High − Low) × TrailFactor` 的跟踪距离。
- 满足突破条件后通过市价单执行，同时仅保留一个净头寸，对应 MQL 中的 `OrdersTotal()` 限制。
- 止损与止盈为虚拟水平：策略监控最优买卖价，当触及虚拟水平时用市价单平仓。

## 交易规则

1. **做多条件**
   - 使用上一根完整日线。
   - 若 `Close > (High + Low) / 2` 且当前 Ask 高于前高，则开多仓。
   - 止损设在前低（按价格步长取整）。
   - 止盈 = 入场价 + `(High − Low) × TakeFactor`。

2. **做空条件**
   - 若 `Close < (High + Low) / 2` 且当前 Bid 低于前低，则开空仓。
   - 止损设在前高，止盈 = 入场价 − `(High − Low) × TakeFactor`。

3. **跟踪止损**
   - 最近一根完成的 `TrailCandleType` K 线提供 `(High − Low) × TrailFactor`。
   - 多头时，止损跟随 `Bid − TrailDistance`，前提是价格距离止盈仍大于经纪商的最小止损距离。
   - 空头时，止损更新为 `Ask + TrailDistance`，条件是价格高于止盈加上最小止损距离。

4. **资金保护**
   - 策略记录账户权益峰值。当权益低于峰值的 50% 时停止交易，与原 EA 行为一致。
   - `CooldownSeconds` 维持 5 秒冷却时间，避免过于频繁地下单或修改止损，对应原始的 `TimeLocal()` 过滤。

## 仓位与风控

- 每笔交易的风险资金 = `Portfolio.CurrentValue × RiskFraction`。
- 通过止损距离及标的参数 (`PriceStep`, `StepPrice`) 计算单合约的货币风险。
- 结果向下对齐到 `Security.VolumeStep`，并限制在 `Security.MinVolume` 与 `Security.MaxVolume` 之间，最终再受 `MaxVolume`（默认 5 手）限制。
- 若规范化后的数量为零，或止损距离小于 `MinStopDistancePoints`（模拟 `MODE_STOPLEVEL`），则放弃下单。

## 参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `SignalCandleType` | 日线 | 用于识别突破的蜡烛类型。 |
| `TrailCandleType` | 4 小时 | 生成跟踪距离的蜡烛类型。 |
| `TakeFactor` | 0.8 | 乘以日线范围得到止盈距离。 |
| `TrailFactor` | 10 | 乘以 4 小时范围得到跟踪距离。 |
| `RiskFraction` | 0.05 | 每笔交易投入的权益比例（5%）。 |
| `MaxVolume` | 5 | 最终下单量的上限。 |
| `MinStopDistancePoints` | 0 | 最小止损/止盈距离（点数），请按经纪商 `MODE_STOPLEVEL` 设置。 |
| `CooldownSeconds` | 5 | 相邻交易操作之间的最小间隔。 |

## 实现说明

- 请确保标的正确设置 `Security.PriceStep`、`Security.StepPrice`、`Security.VolumeStep`、`Security.MinVolume` 以及可选的 `Security.MaxVolume`。
- 止损/止盈逻辑通过监控报价并发出市价单来执行，并不会提交真实的保护性委托。
- 权益监控使用 `Portfolio.CurrentValue`。如果连接器未提供该字段，则风险保护会阻止交易，直至获取到数值。
- 策略只维护单一净仓；持仓期间的反向信号会被忽略。
- 目前仅提供 C# 版本，本目录不包含 Python 实现。
