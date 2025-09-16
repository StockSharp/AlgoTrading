# AMA Trader 策略

## 策略概述
AMA Trader 策略再现了 MetaTrader 5 专家顾问 “AMA Trader” 的核心思想。该策略同时使用考夫曼自适应移动平均线（AMA）和 RSI 指标：当价格保持在 AMA 一侧时，策略会在短期回调出现时进行加仓。StockSharp 版本采用高级 API，通过蜡烛订阅和指标绑定重建原始逻辑，与原策略保持高度一致，同时符合 StockSharp 的交易模型。

## 市场假设
- **交易品种**：外汇、差价合约等允许加仓的趋势型品种。
- **时间框架**：默认 1 分钟蜡烛，可通过 `CandleType` 参数调整。
- **交易时段**：不含额外的时段过滤，每根已完成的蜡烛都会被处理。

## 指标组件
1. **Kaufman Adaptive Moving Average（AMA）**
   - 由 `AmaLength`、`AmaFastPeriod`、`AmaSlowPeriod` 控制平滑速度。
   - 用作趋势过滤器：价格高于 AMA 时只考虑做多，价格低于 AMA 时只考虑做空。
2. **Relative Strength Index（RSI）**
   - 使用 `RsiLength` 周期在蜡烛收盘价上计算。
   - `StepLength` 指定需要检查的 RSI 最近值数量。值为 0 时等同于只检查当前值，符合原始实现。
   - `RsiLevelDown`（默认 30）和 `RsiLevelUp`（默认 70）分别作为超卖、超买阈值。

## 交易流程
1. **蜡烛校验**
   - 仅在蜡烛完成且策略允许交易时运行逻辑。
2. **收益检查（进场前）**
   - 若所有未平仓头寸的浮动收益超过 `ProfitTarget`，立即平掉全部仓位。
   - 若自上次重置以来的已实现收益增加超过 `WithdrawalAmount`，同样平仓并更新收益基准。该机制模拟原版中的提现函数（不会实际扣减账户资金）。
3. **多头逻辑**
   - 条件：收盘价 > AMA 且最近 `StepLength` 个 RSI 值中至少有一个低于 `RsiLevelDown`。
   - 动作：发送买入市价单；如果当前多头组合处于亏损状态（基于平均建仓价计算），则额外再买入一单进行摊薄。
4. **空头逻辑**
   - 条件：收盘价 < AMA 且最近 `StepLength` 个 RSI 值中至少有一个高于 `RsiLevelUp`。
   - 动作：发送卖出市价单；若当前空头组合亏损，则再追加一笔卖单。
5. **仓位跟踪**
   - 在 `OnOwnTradeReceived` 中记录成交量与平均价，分别维护多头与空头的持仓信息，用于计算浮动盈亏。

## 风险控制
- **加仓量**：每次进场使用固定手数 `LotSize`。当仓位亏损时，策略会再增加一单同方向仓位。
- **浮动盈利目标**：`ProfitTarget`（默认 50）到达后强制平掉所有仓位。
- **已实现盈利阈值**：`WithdrawalAmount`（默认 1000）用于监控累计已实现收益，超过阈值后平仓并重置参考值。
- **其他止损**：策略本身不带固定止损/止盈，必要时可结合外部风控模块。

## 参数说明
| 参数 | 含义 |
|------|------|
| `CandleType` | 指标使用的蜡烛类型或时间框架。 |
| `LotSize` | 每笔市价单的固定数量。 |
| `RsiLength` | RSI 计算周期。 |
| `StepLength` | 参与判断的 RSI 最近值数量，0 表示仅检查当前值。 |
| `RsiLevelUp` | RSI 超买阈值。 |
| `RsiLevelDown` | RSI 超卖阈值。 |
| `AmaLength` | AMA 平滑周期。 |
| `AmaFastPeriod` | AMA 快速平滑常数。 |
| `AmaSlowPeriod` | AMA 慢速平滑常数。 |
| `ProfitTarget` | 触发一次性平仓的浮动收益值（0 表示关闭）。 |
| `WithdrawalAmount` | 累计已实现收益达到该值时平仓（0 表示关闭）。 |

## StockSharp 实现细节
- 通过 `SubscribeCandles` 订阅蜡烛数据，并使用 `.Bind` 同时绑定 AMA 与 RSI，处理函数直接接收十进制值，无需手动访问指标缓存。
- 自定义字段在 `OnOwnTradeReceived` 中维护多、空仓位的平均价格和数量，避免使用被限制的聚合接口。
- 进场使用 `BuyMarket` / `SellMarket`，平仓时传入具体数量以同时清理多空头寸。
- 由于蜡烛模式下无法直接获取报价，策略在比较 AMA 时使用蜡烛收盘价，这是最接近原策略的选择。

## 与原版策略的差异
- `WithdrawalAmount` 仅更新内部基准，不会调用 MetaTrader 的 `TesterWithdrawal` 函数。
- 没有提供 AMA 位移和自定义价格源的参数；StockSharp 指标统一使用收盘价。
- 手续费与利息由 StockSharp 的撮合环境处理，代码中未额外调整。

## 使用建议
- 对于加仓策略，应结合账户层面的风险限制或 StockSharp 内置保护功能。
- 针对不同标的优化 AMA 与 RSI 参数。较快的市场通常需要更短的 AMA 周期与更宽的 RSI 阈值。
- 当 `StepLength` 大于 1 时要关注浮亏，因为在强烈反向走势中策略可能连续加仓。
