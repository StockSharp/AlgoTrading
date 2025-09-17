# BB Swing 策略

## 概述

**BB Swing 策略** 是 MetaTrader "BB SWING" 智能交易系统的移植版本。策略利用布林带回落交易，同时通过两条线性加权移动平均线（LWMA）来识别趋势方向。更高周期的动量指标以及超慢速 MACD 用于确认反转是否具备足够的力度。

## 交易逻辑

1. 仅处理 `CandleType` 所指定周期的已完成 K 线。
2. 始终缓存最近四根完成的 K 线，以检查极值位置与实体大小。
3. 当快速 LWMA 高于（做多）或低于（做空）慢速 LWMA 时才考虑开仓。
4. 多头条件要求最近三根 K 线的最低价至少一次触及布林带下轨；空头条件要求最高价至少一次触及上轨。
5. 前一根 K 线的实体必须大于再前一根的实体，表示价格已经离开布林带边缘。
6. 在 `MomentumCandleType` 周期上计算动量指标，取动量与 100 的绝对差值；最近三次读数中任意一次超过设定阈值方可通过多/空过滤。
7. 在 `MacdCandleType` 周期上计算 MACD。做多只有在主线位于信号线上方时才允许，做空则要求主线位于信号线下方。
8. 所有条件满足后，以当前的递增手数执行市价单进入市场。

## 仓位规模与加仓

- `InitialVolume` 决定首次开仓手数。
- 每次追加仓位都会按照 `InitialVolume * LotExponent^n` 的公式放大手数。
- `MaxTrades` 限制最多追加的次数，整体仓位不会超过 `InitialVolume * MaxTrades`。

## 离场与风险控制

- 以价格步长表示的固定 `StopLoss` 与 `TakeProfit`。
- `EnableBreakEven` 启用后，价格前进 `BreakEvenTrigger` 步时把止损移动到 `BreakEvenOffset`。
- `EnableTrailingStop` 会以 `TrailingStop` 步长度跟随最新极值。
- 资金管理模块：
  - `UseMoneyTakeProfit`：当浮盈达到 `MoneyTakeProfit`（账户货币）时平仓。
  - `UsePercentTakeProfit`：当浮盈达到初始权益的 `PercentTakeProfit`% 时平仓。
  - `UseMoneyTrailing`：浮盈超过 `MoneyTrailTarget` 后启动资金追踪，回撤 `MoneyTrailStop` 即平仓。
- `UseEquityStop` 根据会话内记录的权益峰值监控回撤，超过 `EquityRiskPercent`% 即关闭所有仓位。
- `CloseOnMacdCross` 可在 MACD 主线反向穿越信号线时立即离场。

以上所有保护动作都通过 `BuyMarket` / `SellMarket` 市价单一次性平掉整笔仓位。

## 参数

| 名称 | 说明 |
|------|------|
| `InitialVolume` | 初次开仓手数。 |
| `LotExponent` | 每次追加时的递增倍率。 |
| `MaxTrades` | 允许的最大加仓次数。 |
| `TakeProfit` | 以价格步长表示的止盈。 |
| `StopLoss` | 以价格步长表示的止损。 |
| `FastMaPeriod` | 快速 LWMA 周期（使用典型价）。 |
| `SlowMaPeriod` | 慢速 LWMA 周期（使用典型价）。 |
| `MomentumLength` | 动量指标所用的周期数。 |
| `MomentumBuyThreshold` | 多头方向动量与 100 的最小差值。 |
| `MomentumSellThreshold` | 空头方向动量与 100 的最小差值。 |
| `EnableBreakEven` | 是否启用保本移动。 |
| `BreakEvenTrigger` | 触发保本所需的步数。 |
| `BreakEvenOffset` | 保本后设置的止损偏移。 |
| `EnableTrailingStop` | 是否启用价格步长追踪止损。 |
| `TrailingStop` | 追踪止损的步数。 |
| `UseMoneyTakeProfit` | 是否启用以货币计的止盈。 |
| `MoneyTakeProfit` | 货币止盈阈值。 |
| `UsePercentTakeProfit` | 是否按权益百分比止盈。 |
| `PercentTakeProfit` | 触发百分比止盈的比例。 |
| `UseMoneyTrailing` | 是否启用资金追踪。 |
| `MoneyTrailTarget` | 启动资金追踪的浮盈阈值。 |
| `MoneyTrailStop` | 启动后允许的最大回撤金额。 |
| `UseEquityStop` | 是否启用权益回撤保护。 |
| `EquityRiskPercent` | 允许的最大权益回撤百分比。 |
| `CloseOnMacdCross` | 是否根据 MACD 交叉强制离场。 |
| `CandleType` | 产生信号的基础周期。 |
| `MomentumCandleType` | 动量过滤所用的更高周期。 |
| `MacdCandleType` | MACD 过滤所用的慢周期。 |

## 注意事项

- 策略仅对完成的 K 线做出响应，不参与盘中波动。
- 所有价格步长相关的计算依赖于证券的 `PriceStep`，请确保交易所数据正确。
- 资金与权益保护功能需要 StockSharp 提供的投资组合统计，在回测或仿真环境中请确认组合数据已启用。
- 与原版 MQL 程序不同，C# 实现以聚合仓位的方式管理多次加仓，不会创建多个独立订单。
- 布林带使用固定参数：周期 20、宽度 2 个标准差，并采用典型价，与原始脚本保持一致。
