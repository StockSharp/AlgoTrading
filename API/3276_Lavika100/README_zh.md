# Lavika100 策略（StockSharp）

## 概览
**Lavika100** 是 MetaTrader 5 专家顾问 “Lavika  cent” 的完整移植。策略将 1 小时（H1）与 4 小时（H4）的 RAVI 动量过滤器结合，用于挑选进场时机。StockSharp 版本保留了原始 EA 的核心功能：可选的资金管理模式（固定手数或按风险百分比计算）、单仓控制、信号反转开关，以及自动的止损/止盈/追踪保护。所有逻辑均基于高层 API：通过蜡烛订阅驱动流程、指标通过 `Bind` 绑定、`StartProtection` 负责保护性委托。

## 工作流程
1. **数据订阅** – 订阅 H1 蜡烛作为执行周期，H4 蜡烛作为趋势过滤。`SimpleMovingAverage` 指标应用于开盘价，模拟 MT5 中的 `iMA(..., PRICE_OPEN)` 计算方式。
2. **RAVI 动量** – 每个周期都计算快、慢两个移动平均，得到 RAVI 百分比 `(fast - slow) / slow * 100`。只有当 H1 的 RAVI 为正时才考虑入场。
3. **趋势形态识别** – 检查 H4 上最新四个 RAVI 值：
   - 当满足 `r0 > r1`、`r1 < r2`、`r2 < r3` 时视为多头形态；
   - 当满足 `r0 < r1`、`r1 > r2`、`r2 > r3` 时视为空头形态。此逻辑完全复现原始代码的行为，即便原 EA 通常通过 `Reverse` 参数来切换方向。
4. **信号反转与持仓清理** – `ReverseSignals` 与 `CloseOpposite` 控制是否按原方向下单、是否提前平掉反向持仓。
5. **资金管理** – 手数直接取自 `FixedVolume`，或在风险模式下使用 `RiskPercent`（账户价值 * 百分比 / 止损距离）动态计算。
6. **保护措施** – 只要任意保护距离大于零，启动时立即调用 `StartProtection` 设置止损、止盈以及追踪止损和步长。

## 交易规则
- **做多** – H1 RAVI 为正，且 H4 的 RAVI 呈现多头形态；若 `CloseOpposite=true`，会在买入前先平掉空单。
- **做空** – H1 RAVI 为正，且 H4 的 RAVI 呈现空头形态；当 `ReverseSignals=true` 时方向互换，对应 MT5 中的 “Reverse”。
- **单仓模式** – `OnlyOnePosition=true` 时，持仓不为零将阻止再次入场。
- **手数计算** – 风险模式依据 `PriceStep`/`StepPrice` 换算价格到货币价值，并遵守 `VolumeStep`、`VolumeMin`、`VolumeMax`。

## 参数
| 名称 | 说明 |
| --- | --- |
| `H1CandleType` | 执行周期，默认 1 小时。 |
| `H4CandleType` | 趋势过滤周期，默认 4 小时。 |
| `H1FastPeriod` / `H1SlowPeriod` | H1 RAVI 使用的快/慢均线周期。 |
| `H4FastPeriod` / `H4SlowPeriod` | H4 RAVI 使用的快/慢均线周期。 |
| `StopLossPoints` | 止损距离（以 pip 类单位表示）。 |
| `TakeProfitPoints` | 止盈距离（同上）。 |
| `TrailingStopPoints` | 追踪止损距离，设为 0 表示禁用。 |
| `TrailingStepPoints` | 追踪止损的最小步长，启用追踪时必须大于 0。 |
| `FixedVolume` | 固定仓位模式下的手数。 |
| `RiskPercent` | 风险百分比模式下使用的账户百分比。 |
| `MoneyMode` | `FixedLot` 与 `RiskPercent` 的切换。 |
| `OnlyOnePosition` | 是否仅允许单一持仓。 |
| `ReverseSignals` | 是否反转买卖方向（默认 true，与原 EA 相同）。 |
| `CloseOpposite` | 下单前是否平掉反向仓位。 |

## 转换说明
- Pip 换算遵循 MT5 逻辑：三位或五位报价会把 `PriceStep` 乘以 10 作为 pip 单位。
- RAVI 历史仅使用四个可空字段存储，避免创建自定义集合，符合 `AGENTS.md` 对高层 API 的限制。
- 资金管理无需调用被禁止的 `GetValue` 方法，而是使用 StockSharp 的行情元数据完成风险计算。
- 只有在至少一个保护距离为正时才会调用 `StartProtection`，保证在回测与实盘中都安全。

## 使用建议
- 请选择已经正确配置 `PriceStep`、`StepPrice`、`VolumeStep`、`VolumeMin`、`VolumeMax` 的外汇类品种。
- 若启用风险百分比模式，请务必设置非零的 `StopLossPoints`，否则计算出的手数将为零。
- 原版 EA 的信号分支都指向买入。若需要完全复刻其行为，请保持 `ReverseSignals=true`。
