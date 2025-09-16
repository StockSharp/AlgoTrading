# Dealers Trade MACD 策略
[English](README.md) | [Русский](README_ru.md)

本策略移植自 MQL5 的 “Dealers Trade v7.74 MACD” 专家顾问。它是一套顺势加仓系统，通过 MACD 主线的斜率决定多空方向，并在价格沿趋势推进时逐步扩大仓位。作者建议用于 H4、D1 等较高周期，以过滤短周期噪音。

## 策略逻辑

- **信号判定**：订阅所选周期的蜡烛图，在每根收盘 K 线计算 MACD 主线数值。主线向上视为看多，向下视为看空。参数 `ReverseCondition` 可以反转信号，用于需要反向交易的账户。
- **仓位控制**：第一笔订单使用固定手数 `FixedVolume`。若该值为 0，则改为使用账户当前权益 * `RiskPercent`（百分比）÷ 止损距离的方式动态确定手数。后续加仓单的体积按照 `VolumeMultiplier^(当前仓位数量)` 递增，同时要求价格距离上一次成交至少 `IntervalPoints * PriceStep`。若加仓后仓位数量超过 `MaxPositions` 或总量超过 `MaxVolume`，则放弃该信号。
- **仓位管理**：每笔持仓都记录独立的止损、止盈价格，根据参数 `StopLossPoints`、`TakeProfitPoints` 计算（单位为最小报价步长）。当 `TrailingStopPoints` 大于 0 时，在浮盈超过 `TrailingStopPoints + TrailingStepPoints` 后启动追踪止损，模拟原始 EA 的移动保护逻辑。
- **账户保护**：当持仓数量大于 `PositionsForProtection` 且浮盈合计达到 `SecureProfit` 时，策略会先平掉盈利最高的那笔仓位锁定收益，然后再考虑继续加仓。

## 参数说明

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | H4 | 计算信号的蜡烛周期。 |
| `FixedVolume` | 0.1 | 第一笔订单的手数。设为 0 时启用按风险百分比的动态手数。 |
| `RiskPercent` | 5 | 当 `FixedVolume = 0` 时，允许冒的权益百分比。 |
| `StopLossPoints` | 90 | 止损距离（按价格最小变动单位计）。0 表示不下止损。 |
| `TakeProfitPoints` | 30 | 止盈距离（价格步长单位）。0 表示不设定。 |
| `TrailingStopPoints` | 15 | 移动止损的基础距离。0 表示关闭追踪。 |
| `TrailingStepPoints` | 5 | 每次更新移动止损前，额外需要的利润空间。 |
| `MaxPositions` | 5 | 最多允许的加仓次数。 |
| `IntervalPoints` | 15 | 相邻加仓所需的最小价格间隔（单位：价格步长）。 |
| `SecureProfit` | 50 | 触发账户保护的浮盈阈值（报价货币）。 |
| `AccountProtection` | true | 是否启用账户保护机制。 |
| `PositionsForProtection` | 3 | 账户保护生效所需的最少持仓数量。 |
| `ReverseCondition` | false | 是否反转 MACD 方向判断。 |
| `MacdFastPeriod` | 14 | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | 26 | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | 1 | MACD 信号 EMA 周期（与原始 EA 相同）。 |
| `MaxVolume` | 5 | 累计仓位数量上限。 |
| `VolumeMultiplier` | 1.6 | 每次加仓的手数倍增系数。 |

## 注意事项

- MQL 版本允许同时持有多头与空头（对冲模式）。StockSharp 默认使用净额持仓，因此本移植版在反向开仓前会先平掉相反方向的仓位。
- 策略只在蜡烛收盘后评估 MACD，因此不会像逐笔行情那样对瞬时波动做出反应，但更适合历史测试与实盘验证。
- 所有“点数”参数都会乘以交易品种的 `PriceStep`。若品种没有提供该信息，则退回到 0.0001 的默认步长，请在必要时调整参数。
- 当 `FixedVolume = 0` 且未设置止损距离时，无法计算风险，策略会跳过交易。

