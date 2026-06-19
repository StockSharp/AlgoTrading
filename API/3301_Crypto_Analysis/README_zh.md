# Crypto Analysis 策略

## 概述
本策略是 MetaTrader 4 智能交易系统「Crypto Analysis」的 StockSharp 移植版。它关注价格触碰主图时间框架的布林带外轨后出现的突破，同时要求结构保持空头倾向（快线 LWMA 低于慢线 LWMA）。只有当高一级时间框架的动量和月度 MACD 同时给出同向信号时才允许开仓。持仓管理沿用了原 EA 的多层防护：点差止损、资金追踪、移动到保本以及账户回撤限制。

## 交易逻辑
- **信号时间框架：** 可配置（默认 M15），所有入场与出场规则均在该时间框架上评估。
- **波动触发：** 上一根 K 线的最低价触及或跌破布林带下轨（20, 2）时准备做多；最高价触及或突破上轨时准备做空。
- **趋势过滤：** 快速 LWMA（默认 6）必须低于慢速 LWMA（默认 85），复制了 MQL 代码中的「下行结构」检查。
- **RSI 确认：** 多头需要 RSI(14) 高于 50，空头需要 RSI(14) 低于 50。
- **动量爆发：** 取高时间框架上最近三个 Momentum(14) 与 100 基准的最大绝对偏离值，必须超过对应的买入/卖出阈值。
- **月度 MACD：** 额外订阅 30 天（默认）K 线，计算 MACD(12, 26, 9)。多头要求主线高于信号线，空头要求主线低于信号线。
- **执行方式：** 当所有过滤器满足条件时发送市价单。若已有反向仓位，先行平仓以保持单一净仓，与原始 EA 的处理一致。

## 仓位管理
- **初始止损/止盈：** 以点数表示的距离会根据合约的最小价格步长自动换算；对于 5 位或 3 位报价（`0.00001`、`0.001`）会乘以 10。
- **移动止损：** 当价格创出新的高点/低点后，止损会按 `TrailingStopPips` 的距离向后跟随，并保留最优位置。
- **保本保护：** 浮盈达到 `BreakEvenTriggerPips` 时，止损移动到入场价并加上/减去 `BreakEvenOffsetPips`。
- **资金目标：** 可以设置绝对金额或百分比的盈利目标，一旦达到立即平仓。
- **资金追踪：** 当浮盈超过 `MoneyTrailTarget` 时，记录最高盈利；若回撤达到 `MoneyTrailStop`，则平仓锁定收益。
- **权益止损：** 监控组合权益（账户价值 + 浮动盈亏），当从峰值回撤超过 `EquityRiskPercent` 时强制平仓。

## 多时间框架数据
策略会自动订阅以下数据源：
1. 主图时间框架的 K 线，用于布林带、LWMA 与 RSI 判断。
2. 更高时间框架（默认 H1）的 K 线，用于 Momentum 过滤。
3. 月度（默认 30 天）K 线，用于 MACD 趋势确认。

## 参数
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 每次入场的基础手数；反向信号出现时会先行平掉当前仓位。 |
| `UseMoneyTakeProfit` | 启用绝对金额止盈。 |
| `MoneyTakeProfit` | 启用金额止盈时的目标利润。 |
| `UsePercentTakeProfit` | 启用按初始权益计算的百分比止盈。 |
| `PercentTakeProfit` | 百分比止盈所需的收益率。 |
| `EnableMoneyTrailing` | 开启资金追踪功能。 |
| `MoneyTrailTarget` | 触发资金追踪的利润阈值。 |
| `MoneyTrailStop` | 资金追踪启动后允许的利润回撤。 |
| `StopLossPips` | 初始止损点数。 |
| `TakeProfitPips` | 初始止盈点数。 |
| `TrailingStopPips` | 移动止损点数。 |
| `UseBreakEven` | 启用保本移动。 |
| `BreakEvenTriggerPips` | 启动保本前需要的浮动点数。 |
| `BreakEvenOffsetPips` | 保本时在入场价基础上的额外点数。 |
| `FastMaPeriod` | 快速 LWMA 的周期（使用典型价）。 |
| `SlowMaPeriod` | 慢速 LWMA 的周期（使用典型价）。 |
| `MomentumPeriod` | 高时间框架上 Momentum 指标的周期。 |
| `MomentumBuyThreshold` | 多头信号需要的最小动量偏离。 |
| `MomentumSellThreshold` | 空头信号需要的最小动量偏离。 |
| `MacdFastLength` | MACD 滤波器的快速 EMA 周期。 |
| `MacdSlowLength` | MACD 滤波器的慢速 EMA 周期。 |
| `MacdSignalLength` | MACD 滤波器的信号线周期。 |
| `UseEquityStop` | 启用账户权益止损。 |
| `EquityRiskPercent` | 允许的最大权益回撤百分比。 |
| `CandleType` | 主图时间框架。 |
| `MomentumCandleType` | Momentum 所使用的时间框架。 |
| `MacdCandleType` | MACD 所使用的时间框架。 |

## 备注
- 该移植版保持单一净仓，与原 EA 在开反向单前先平仓的行为一致。
- 所有防护规则均在 K 线收盘时评估，模拟原脚本的“新 K 线”逻辑。
- 若交易标的使用非标准报价步长，请根据合约规格调整点数相关的参数。
