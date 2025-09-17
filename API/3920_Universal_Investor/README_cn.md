# Universal Investor 策略

本策略直接移植自 MetaTrader 4 平台上的 **Universal Investor** 智能交易系统。通过结合指数移动平均线（EMA）与线性加权移动平均线（LWMA）来确认短期趋势，只保持单向持仓并根据风险动态调整下单手数。

## 交易逻辑

1. 订阅参数 `CandleType` 指定的 K 线，并使用 `MovingPeriod` 设置的周期计算 EMA 与 LWMA。
2. 保存每条均线最近两个数值，从而复刻原始 EA 中 `iMA(..., shift = 1/2)` 的行为。
3. 当上一根 LWMA 位于上一根 EMA 之上、且两条均线同时上升且不存在相反信号时，产生**做多**信号。
4. 当上一根 LWMA 位于上一根 EMA 之下、且两条均线同时下降且不存在相反信号时，产生**做空**信号。
5. 当 LWMA 跌破 EMA 时立即平掉多头仓位（做空仓位使用对称逻辑）。
6. 基于策略的 `Volume` 参数计算下单量；若账户权益满足 `MaximumRisk` 条件则提升仓位，若出现连续亏损则按照 `DecreaseFactor` 递减仓位。
7. 使用 `BuyMarket`/`SellMarket` 提交市价单，并记录入场价以判断平仓盈亏。

策略同一时间只持有一笔仓位，并且必须先完全平仓才会开出反向订单，与原版 MetaTrader 脚本的行为一致。

## 参数

| 名称 | 说明 |
| --- | --- |
| `CandleType` | 用于计算的 K 线数据类型。 |
| `MovingPeriod` | EMA 与 LWMA 的计算周期。 |
| `MaximumRisk` | 按账户权益计算最小下单量的风险系数（0.05 表示 5%）。 |
| `DecreaseFactor` | 连续亏损后降低仓位的系数（0 表示不启用）。 |
| `Volume` | 传递给 `BuyMarket`/`SellMarket` 的基础手数。 |

## 指标

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## 说明

- 所有决策都在 K 线收盘后执行，对应原始 EA 中对 `Time[0]` 的检查。
- 仓位管理逻辑复刻了 `LotsOptimized` 函数，包括风险占用以及根据亏损次数递减手数的部分。
