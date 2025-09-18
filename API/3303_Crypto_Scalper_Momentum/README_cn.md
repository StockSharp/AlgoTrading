# Crypto Scalper Momentum 策略

## 概览

**Crypto Scalper Momentum** 策略将 MetaTrader 上的 "Crypto Scalper" EA 移植到 StockSharp 平台。策略同时使用三个时间框架：

- 主时间框架：Money Flow Index 与 MACD 用于生成基础信号；
- 较高时间框架：Momentum 衡量价格动量是否足够强；
- 宏观时间框架：慢速 MACD 过滤长期趋势，若趋势反转则立即平仓。

原始 EA 中的资金管理模块同样得到保留，包括货币止盈、利润回撤跟踪、无损保护以及权益回撤监控。

## 交易逻辑

1. **主时间框架指标**
   - MFI（默认 14）判断超买超卖区域；
   - MACD（12/26/9）提供方向确认。
2. **高时间框架动量**
   - Momentum 指标来自单独的蜡烛订阅，只有当绝对偏离 MetaTrader 参考值 100 超过阈值时才允许开仓。
3. **宏观趋势过滤器**
   - 慢速 MACD（默认日线）限制逆势交易，同时在信号翻转时强制平掉所有仓位。
4. **入场条件**
   - **做多**：最近三次 MFI 至少一次低于超卖阈值；Momentum 偏离超过阈值；主 MACD 在信号线上方；宏观 MACD 仍然看多。
   - **做空**：相反条件（MFI 超买、MACD 走弱、宏观 MACD 看空）。
5. **离场条件**
   - 固定止损与止盈（以点数表示）。
   - 根据最近蜡烛极值或经典点差进行追踪止损。
   - 达到设定利润后将止损移至无损。
   - 宏观 MACD 反向时立即平仓。
   - 货币目标、百分比目标及利润回撤跟踪与原版一致。
   - 监控账户权益高点并在回撤超过设定比例时清仓。

## 风险管理

- **Stop/Take**：可选的固定止损、止盈距离。
- **Trailing**：在蜡烛极值和经典追踪之间切换。
- **Break-even**：达到设定盈利后将止损移至成本价上方/下方。
- **Money Management**：支持货币止盈、百分比止盈及浮动利润回撤止损。
- **Equity Stop**：当权益回撤超过允许比例时强制平仓。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 主时间框架蜡烛。 |
| `MomentumCandleType` | 计算 Momentum 的时间框架。 |
| `MacroCandleType` | 宏观 MACD 所用时间框架。 |
| `MfiPeriod` | MFI 周期。 |
| `MfiOversold` / `MfiOverbought` | 超卖 / 超买阈值。 |
| `MomentumPeriod` | Momentum 周期。 |
| `MomentumThreshold` | 与 100 的最小偏离。 |
| `MomentumReference` | Momentum 参考值（默认 100）。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | 主 MACD 参数。 |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | 宏观 MACD 参数。 |
| `TradeVolume` | 单次下单手数。 |
| `MaxTrades` | 同向最多持仓数量（0 表示无限制）。 |
| `UseStopLoss` / `StopLossPips` | 启用与设置止损。 |
| `UseTakeProfit` / `TakeProfitPips` | 启用与设置止盈。 |
| `UseTrailingStop` | 启用追踪止损。 |
| `UseCandleTrail` | 使用蜡烛极值追踪。 |
| `TrailTriggerPips` / `TrailAmountPips` | 经典追踪触发距离与保持距离。 |
| `CandleTrailLength` / `CandleTrailBufferPips` | 蜡烛追踪的回溯数量与缓冲。 |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | 无损保护触发条件与锁定利润。 |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | 以账户货币计算的总体止盈。 |
| `UsePercentTakeProfit` / `PercentTakeProfit` | 按初始权益百分比计算的总体止盈。 |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | 浮动利润追踪。 |
| `UseEquityStop` / `EquityRiskPercent` | 权益回撤限制。 |
| `ForceExit` | 下一根蜡烛收盘时强制平仓。 |

## 补充说明

- 点数通过 `PriceStep` 换算为价格增量。若合约未提供价格步长，则使用 `0.0001` 作为默认值，以匹配 MetaTrader 的 `Point()` 行为。
- 若数据源提供月线，可将 `MacroCandleType` 切换为月线以完全复现原策略；默认使用日线以提升兼容性。
- 代码中的注释全部使用英文，以满足仓库规范。
