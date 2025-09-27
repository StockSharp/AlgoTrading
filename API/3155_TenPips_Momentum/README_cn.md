# TenPips 动量策略

**TenPips** 策略是 MetaTrader "10PIPS" 智能交易系统的 StockSharp 高级 API 版本。它在交易周期上计算快/慢线性加权均线，并结合高周期动量确认以及月度 MACD 过滤器。风险管理模块保留了原有 EA 的功能：点差止损/止盈、移动止损、保本、资金追踪以及权益止损。

## 信号逻辑

1. **基础周期**（`CandleType`，默认 15 分钟）提供价格数据，并按典型价 `(High + Low + Close) / 3` 计算快慢 LWMA。
2. **动量周期**（`MomentumCandleType`，默认 1 小时）把 StockSharp 的动量差值转换成 MetaTrader 的比值 `Close / Close(period) * 100`。最近三个完成 K 线中任意一个与 100 的绝对偏差需大于 `MomentumThreshold` 才允许交易。
3. **宏观 MACD 周期**（`MacdCandleType`，默认 30 天蜡烛，近似 MT4 月线）用于判定趋势：多头要求 MACD 主线高于信号线，空头则相反。

多头条件：
- 上一根 K 线先跌破再收在快线之上；
- 快线位于慢线上方；
- 最近三个高周期动量读数中至少一个大于 `MomentumThreshold`；
- 月度 MACD 为多头。

空头条件完全对称：上一根 K 线收在快线之下、快线在慢线之下、动量满足阈值且 MACD 看空。

StockSharp 采用净持仓模型，同一时刻仅维持一个方向的总仓位。如果在空头状态下收到买入信号，成交量会先平掉空单，再建立指定规模的多单。

## 风险与资金管理

- **固定距离**：`StopLossPips` 与 `TakeProfitPips` 按 `PriceStep` 将 MT4 “点”转换为价格偏移，触达后以市价平仓。
- **追踪止损**：`TrailingStopPips` 根据入场后的最高/最低价动态移动止损。
- **保本**：启用 `UseBreakEven` 时，达到 `BreakEvenTriggerPips` 后将止损移至入场价，并可附加 `BreakEvenOffsetPips` 点的安全余量。
- **资金目标**：`UseMoneyTakeProfit`、`UsePercentTakeProfit`、`EnableMoneyTrailing` 对应 EA 的现金止盈、百分比止盈与资金追踪模块，使用蜡烛收盘价估算浮动盈亏。
- **权益止损**：`UseEquityStop` 与 `EquityRiskPercent` 复制 EA 的权益保护逻辑，当从权益峰值回撤超过阈值时强制平仓。
- **MACD 退出**：`UseMacdExit` 映射原策略的 `Exit` 开关，当月度 MACD 翻转时提前离场。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | `0.01` | 市价单成交量（等同于 MT4 手数）。 |
| `CandleType` | 15 分钟 | 主交易周期。 |
| `MomentumCandleType` | 1 小时 | 动量确认周期。 |
| `MacdCandleType` | 30 天 | MACD 过滤周期（近似月线）。 |
| `FastMaPeriod` | `8` | 快速 LWMA 周期。 |
| `SlowMaPeriod` | `50` | 慢速 LWMA 周期。 |
| `MomentumPeriod` | `14` | 动量回溯长度。 |
| `MomentumThreshold` | `0.3` | 动量相对 100 的最小偏差。 |
| `StopLossPips` | `20` | 点差止损，0 表示禁用。 |
| `TakeProfitPips` | `50` | 点差止盈，0 表示禁用。 |
| `TrailingStopPips` | `40` | 点差追踪止损距离。 |
| `UseBreakEven` | `true` | 是否启用保本。 |
| `BreakEvenTriggerPips` | `30` | 保本触发所需盈利（点）。 |
| `BreakEvenOffsetPips` | `30` | 保本后附加的余量（点）。 |
| `UseMoneyTakeProfit` | `false` | 启用现金止盈。 |
| `MoneyTakeProfit` | `10` | 现金止盈目标（账户货币）。 |
| `UsePercentTakeProfit` | `false` | 启用百分比止盈。 |
| `PercentTakeProfit` | `10` | 相对于初始权益的百分比目标。 |
| `EnableMoneyTrailing` | `true` | 启用资金追踪（`MoneyTrailTarget` / `MoneyTrailStop`）。 |
| `MoneyTrailTarget` | `40` | 资金追踪启动所需盈利。 |
| `MoneyTrailStop` | `10` | 资金追踪允许的回撤。 |
| `UseEquityStop` | `true` | 启用权益止损。 |
| `EquityRiskPercent` | `1` | 从权益峰值允许的最大回撤（%）。 |
| `UseMacdExit` | `false` | 当月度 MACD 反转时是否提前离场。 |

## 实现细节

- 点值换算遵循原 EA：当 `PriceStep` 为 `0.00001` 或 `0.001` 时，一个点等于 10 个最小跳动，否则直接使用报价步长。
- StockSharp 的动量输出为价格差，策略会转换成 MT4 使用的比值后再比较阈值。
- 由于净持仓模式，未实现 EA 的多单累加与马丁倍数（`IncreaseFactor`、`LotExponent`、`Max_Trades`），反向开仓时会自动包含平仓量。
- 所有保护和盈利操作均使用市价单，贴合原机器人对订单的处理方式。
- 可视化时会绘制主周期蜡烛、两条 LWMA、动量指标与 MACD。

## 使用步骤

1. 根据原策略配置 `CandleType`、`MomentumCandleType` 与 `MacdCandleType` 的时间框架。
2. 按品种步长设置 `StopLossPips`、`TakeProfitPips`、`TrailingStopPips` 与保本参数，0 表示禁用。
3. 根据风险偏好启用或关闭现金/百分比止盈、权益止损和 MACD 退出开关。
4. 启动策略，系统会自动订阅三个时间框架的数据，执行原始 EA 的进出场逻辑，并在触发各类保护条件时记录并平仓。
