# FIBO1 策略（MQL 24845 移植）

## 概述

**FIBO1 Strategy** 将 Aharon Tzadik 编写的 `FIBO1.mq4` 专家顾问迁移到
StockSharp 高级 API。策略基于单一交易品种，并使用三组过滤器：

1. **趋势过滤** – 典型价的快/慢线性加权均线（LWMA）。做多要求快线位于慢线之上，
   做空则相反。
2. **动量确认** – 连续三根动量值与买入/卖出阈值比较，保持 MQL 版本中“偏离100的
   绝对值”这一逻辑。
3. **MACD 过滤** – 在更高周期上计算 MACD，只有当主线位于信号线上方（多头）或
   下方（空头）时才允许开仓。

持仓后，策略重现 `FIBO1.mq4` 中复杂的离场模块：

- 以点数表示的止盈/止损。
- 货币金额和百分比目标，以及浮动利润的资金拖尾管理。
- 依据最近若干根K线极值的拖尾止损（包含 PAD AMOUNT 的偏移量）。
- 固定距离的经典拖尾。
- 启动条件可配置的保本保护。
- 监控历史最高权益的回撤，并触发全局平仓的权益风控。

> **提示：** MQL 原版在实盘模式下依赖用户绘制的 "FIBO" 线。StockSharp 无法访问
> 终端图形对象，因此移植版本始终采用测试分支（忽略 Fibo 过滤）。其它功能均已
> 迁移，并在下文详细说明。

## 交易逻辑

1. **信号判定**
   - 等待主时间框架的收盘K线。
   - 检查快LWMA与慢LWMA的相对位置。
   - 复现 `Low[2] < High[1]`（多）与 `Low[1] < High[2]`（空）的K线形态过滤。
   - 计算最近三根动量值与100之间的最大偏离，并与阈值比较。
   - 确认高周期 MACD 主线与信号线的相对顺序。
   - 所有条件满足时，如有反向持仓先平掉，再按设定手数以市价进场。

2. **风险控制**
   - 每次建仓立即调用 StockSharp 保护接口设置点数止盈/止损。
   - 到达激活点后将止损移动到保本价，并可额外加点差。
   - 拖尾逻辑可选择“按K线极值+缓冲”或“固定点差”两种模式。
   - 资金管理模块负责货币目标、百分比目标和浮动利润拖尾。
   - 权益风控记录历史最高权益并在回撤超限时强制平仓。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `UseMoneyTakeProfit` | `false` | 达到 `MoneyTakeProfit`（账户货币）时全部平仓。 |
| `MoneyTakeProfit` | `10` | 货币止盈目标，仅在 `UseMoneyTakeProfit = true` 时生效。 |
| `UsePercentTakeProfit` | `false` | 启用相对于初始权益的百分比止盈。 |
| `PercentTakeProfit` | `10` | 百分比止盈阈值。 |
| `EnableMoneyTrailing` | `true` | 浮盈达到 `MoneyTrailTarget` 后启动资金拖尾。 |
| `MoneyTrailTarget` | `40` | 启动资金拖尾所需的最小浮盈。 |
| `MoneyTrailStop` | `10` | 资金拖尾激活后允许回吐的最大金额。 |
| `UseEquityStop` | `true` | 启用权益回撤保护。 |
| `EquityRiskPercent` | `1` | 允许的最大回撤，占历史最高权益的百分比。 |
| `TradeVolume` | `1` | 市价单的基础手数。 |
| `FastMaPeriod` | `20` | 快速 LWMA 的周期。 |
| `SlowMaPeriod` | `100` | 慢速 LWMA 的周期。 |
| `MomentumPeriod` | `14` | 动量指标的周期。 |
| `MomentumBuyThreshold` | `0.3` | 多头动量偏离阈值。 |
| `MomentumSellThreshold` | `0.3` | 空头动量偏离阈值。 |
| `MacdFastPeriod` | `12` | MACD 快速 EMA 的周期。 |
| `MacdSlowPeriod` | `26` | MACD 慢速 EMA 的周期。 |
| `MacdSignalPeriod` | `9` | MACD 信号 EMA 的周期。 |
| `TakeProfitPips` | `50` | 止盈距离（点）。 |
| `StopLossPips` | `20` | 止损距离（点）。 |
| `TrailingActivationPips` | `40` | 固定距离拖尾的启动盈利（点）。 |
| `TrailingDistancePips` | `40` | 固定距离拖尾保持的点差。 |
| `UseCandleTrailing` | `true` | 使用K线极值拖尾；关闭后改为固定点差。 |
| `CandleTrailingLength` | `3` | 计算拖尾极值时包含的收盘K线数量。 |
| `CandleTrailingOffsetPips` | `3` | K线极值拖尾的额外偏移（点）。 |
| `MoveToBreakEven` | `true` | 启用保本保护。 |
| `BreakEvenActivationPips` | `30` | 触发保本的盈利（点）。 |
| `BreakEvenOffsetPips` | `30` | 保本时附加的偏移量（点）。 |
| `CandleType` | `15m` | 主信号时间框架。 |
| `MomentumCandleType` | `15m` | 动量指标使用的时间框架。 |
| `MacdCandleType` | `1d` | MACD 使用的高周期。 |

## 使用说明

- 默认时间框架遵循原始 EA 的多周期思路：主图与动量使用相同的周期，MACD 采用更
  高一档的周期（默认为日线）。
- 点值换算会在三位/五位报价的外汇品种上自动乘以10，其它品种直接使用 `PriceStep`。
- 策略仅处理收盘K线，数据源必须提供 `Finished` 状态。
- 在净持仓模式下，开反向仓位会先平掉旧仓，再按设定手数建新仓，与 MT4 版本一致。

## 与原版的差异

- 由于无法读取 MT4 图形对象，不再检测手动绘制的 "FIBO" 线，策略始终沿用测试
  分支逻辑。
- `Lots`、`LotExponent`、`Max_Trades` 等参数被统一为 `TradeVolume`，因为 StockSharp
  使用净头寸模型；如需马丁倍量，可通过外部优化器实现。
- 为保持示例精炼，移除了 `Alert`、`SendMail`、`SendNotification` 等通知函数。

在这些调整下，移植版本完整保留了 `FIBO1.mq4` 的交易思想，并提供了详尽的参数
说明，方便在 StockSharp 环境中测试与扩展。
