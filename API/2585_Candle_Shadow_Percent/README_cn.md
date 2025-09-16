# 烛影比例策略

## 概述
**烛影比例策略** 是 MetaTrader 专家顾问 *Candle shadow percent* 的移植版本。策略会寻找上下影线达到可调比例的 K 线：出现长上影线时开空，出现长下影线时开多。交易方向与原始算法一致，同时保留了风险管理流程。

## 转换说明
* 原策略依赖自定义指标。StockSharp 版本直接根据收盘完成的 K 线计算影线与实体比例，无需外部指标。
* 点值通过 `Security.PriceStep` 计算。请根据交易品种调整 `StopLossPips`、`TakeProfitPips` 与 `MinBodyPips`。
* 资金管理按照 MetaTrader 中 `CMoneyFixedMargin` 的思想实现：使用账户当前权益的一定百分比除以止损距离得到下单数量。

## K 线筛选条件
满足以下条件的 K 线才会触发信号：
1. 绝对实体长度不少于 `MinBodyPips * Security.PriceStep`。
2. 对应影线长度为正值。
3. 影线与实体的比例满足阈值逻辑：
   * **上影线（做空）**：当 `TopShadowIsMinimum = true` 时要求 `(High − max(Open, Close)) / Body * 100 ≥ TopShadowPercent`；反之要求该比例小于或等于阈值。
   * **下影线（做多）**：当 `LowerShadowIsMinimum = true` 时要求 `(min(Open, Close) − Low) / Body * 100 ≥ LowerShadowPercent`；反之要求该比例小于或等于阈值。
4. 如果同一根 K 线同时满足多空条件，策略仅保留比例更大的方向，避免重复下单。

## 入场规则
* **空头**：当出现有效的上影线信号且当前为空仓或持有多单时执行。若持有多单会自动反手，并立即设置止损止盈。
* **多头**：当出现有效的下影线信号且当前为空仓或持有空单时执行。若持有空单会先平仓再开多。

## 出场规则
* **止损**：距离入场价 `StopLossPips * Security.PriceStep`。多单止损位于 `entry − stopDistance`，空单止损位于 `entry + stopDistance`。
* **止盈**：距离入场价 `TakeProfitPips * Security.PriceStep`。当 `TakeProfitPips = 0` 时停用止盈，仅依靠止损或反向信号离场。
* 策略只在 K 线收盘后评估。如果收盘 K 线触及止损或止盈，持仓会在下一次处理时关闭。

## 仓位控制
* 每笔交易风险 = `Portfolio.CurrentValue * (RiskPercent / 100)`。若账户权益不可用，则回退到策略配置的默认手数。
* 下单数量 = 风险金额 / 止损距离。若需要反手，会额外加上当前仓位的绝对值，确保完全对冲原仓位，这与原 MQL 实现一致。

## 参数说明
| 参数 | 含义 |
|------|------|
| `CandleType` | 订阅的 K 线数据类型或周期。 |
| `StopLossPips` | 以点/跳动计的止损距离，必须大于 0。 |
| `TakeProfitPips` | 以点/跳动计的止盈距离，0 表示不开启止盈。 |
| `RiskPercent` | 每笔交易承担的账户百分比风险。 |
| `MinBodyPips` | 触发信号所需的最小实体长度（点/跳动）。 |
| `EnableTopShadow` | 是否启用基于上影线的做空信号。 |
| `TopShadowPercent` | 上影线与实体比例的阈值。 |
| `TopShadowIsMinimum` | `true` 表示比例需大于等于阈值，`false` 表示比例需小于等于阈值。 |
| `EnableLowerShadow` | 是否启用基于下影线的做多信号。 |
| `LowerShadowPercent` | 下影线与实体比例的阈值。 |
| `LowerShadowIsMinimum` | 控制下影线阈值是最小值条件还是最大值条件。 |

## 使用建议
* 可先使用原 EA 相似的周期（如 5 分钟），再根据品种微调点数参数。
* 如果噪声过多，可适当提高 `MinBodyPips`；若希望捕捉更细小的反转，可降低该值。
* 需要更多过滤条件时，可在 `OnStarted` 中绑定额外指标。
* 在真实账户前请先于模拟环境验证跳动值与风险设置是否正确。
