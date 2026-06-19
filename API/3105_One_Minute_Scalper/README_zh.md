[English](README.md) | [Русский](README_ru.md)

该策略将 **1 MINUTE SCALPER** MetaTrader 4 智能交易系统迁移到 StockSharp 的高级 API。策略保留了原始 EA 的多级趋势确认、跨周期动量筛选以及月度 MACD 过滤，同时按照 StockSharp 的净持仓模式重写风险控制。

## 核心逻辑

1. **LWMA 梯队** —— 13 条线性加权移动平均线（3/5/8/10/12/15/30/35/40/45/50/55/200）必须严格按顺序排列。做多要求周期越短的均线依次位于下一条之上，做空条件相反。
2. **主趋势过滤** —— 额外的一条快速 LWMA（默认 6）必须位于慢速 LWMA（默认 85）之上方可做多，做空则要求快速线位于慢速线下方。
3. **K 线结构** —— 仅当出现与原脚本一致的重叠结构时才允许进场：多头需要两根 K 线之前的最低价低于上一根 K 线的最高价；空头则要求上一根最低价跌破前两根的最高价。
4. **动量过滤** —— 在更高周期（默认 15 分钟）上计算的 14 周期 Momentum 指标，最近三个值任意一个相对 100 的绝对偏差必须超过设定阈值，对应原代码中的 `MomLevelB/MomLevelS` 判断。
5. **月度 MACD 偏向** —— 在 `MacdCandleType`（默认 30 天，近似月线）上计算的 MACD 主线必须位于信号线之上才能做多，反之做空。

## 仓位管理

- **初始保护** —— 止损与止盈使用价格步长表示。开仓后根据 `Security.PriceStep` 转换为绝对价格。
- **保本移动** —— 当浮动利润达到 `BreakEvenTriggerSteps`（以步长计）时，将止损移动到开仓价并加入 `BreakEvenOffsetSteps` 的额外空间，空头逻辑镜像处理。
- **步长追踪** —— `TrailingStopSteps` 大于零时，止损会跟随自入场以来的最高/最低价，距离为指定步长。
- **资金追踪** —— 浮动盈亏超过 `MoneyTrailTarget`（账户货币）后记录利润峰值，一旦回撤达到 `MoneyTrailStop` 即平仓。
- **资金/百分比目标** —— 可选的绝对或百分比止盈在浮动盈亏达到目标时关闭仓位。百分比目标基于策略启动时的账户价值计算。
- **权益止损** —— 跟踪账户权益高点（投资组合价值 + 未实现盈亏）。若回撤超过 `EquityRiskPercent`，立即清空仓位，对应 EA 中的 `AccountEquityHigh()` 保护。

## 参数

| 参数 | 说明 |
| --- | --- |
| `Volume` | 开仓手数。切换方向时会加上当前净头寸，实现一步反向。 |
| `FastMaPeriod` / `SlowMaPeriod` | 主趋势过滤的 LWMA 长度。 |
| `MomentumPeriod` | 高周期 Momentum 的长度。 |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | 判定做多/做空所需的最低动量偏离度。 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD 在 `MacdCandleType` 上的三组参数。 |
| `StopLossSteps` / `TakeProfitSteps` | 止损、止盈距离（价格步长，0 表示禁用）。 |
| `TrailingStopSteps` | 步长追踪的距离（0 表示关闭）。 |
| `BreakEvenTriggerSteps` / `BreakEvenOffsetSteps` | 移动到保本所需的利润距离及额外偏移。 |
| `UseMoneyTakeProfit`, `MoneyTakeProfit` | 启用并设置资金止盈。 |
| `UsePercentTakeProfit`, `PercentTakeProfit` | 启用并设置相对初始权益的百分比止盈。 |
| `EnableMoneyTrailing`, `MoneyTrailTarget`, `MoneyTrailStop` | 资金追踪的触发与回撤参数。 |
| `UseEquityStop`, `EquityRiskPercent` | 启用权益止损并指定最大回撤百分比。 |
| `CandleType` | 主交易周期（默认 1 分钟）。 |
| `MomentumCandleType` | Momentum 指标使用的周期（默认 15 分钟）。 |
| `MacdCandleType` | MACD 指标使用的周期（默认 30 天 ≈ 月线）。 |

## 与 MT4 原版的差异

- StockSharp 采用净持仓模式，因此策略始终只维护一个总仓，而不是最多 `Max_Trades` 个独立订单。反向时会先平掉当前仓位再开反向仓。
- `PercentTakeProfit` 以策略启动时的账户价值为基准，而不是 MetaTrader 中随时变化的 `AccountBalance()`，可避免外部交易导致的误触发。
- `Take_Profit_In_Money` 与 `TRAIL_PROFIT_IN_MONEY2` 在 StockSharp 中基于平均开仓价计算实时浮动盈亏，实现方式与原 EA 一致但遵循平台的保护单机制。
- 需要确保数据源能够提供 `CandleType`、`MomentumCandleType`、`MacdCandleType` 对应的 K 线周期，否则指标无法形成。

请根据交易品种的波动性调整参数。对于高噪声或点差较大的市场，可适当放宽步长距离或提高动量阈值，以减少噪声信号。
