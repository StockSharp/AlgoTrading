# Broadening Top 策略

## 概述
Broadening Top 策略源自 MetaTrader 平台上的 "Broadening top" 智能交易系统。该策略通过趋势过滤、动量确认和 MACD 指示，在价格形成扩张形态后寻找突破机会。核心组件包括两条线性加权移动平均线（LWMA）、Momentum 指标以及 MACD 过滤器。

## 交易逻辑
1. **趋势过滤**：只有当快速 LWMA 位于慢速 LWMA 上方时才允许做多，当快速 LWMA 位于慢速 LWMA 下方时才允许做空。
2. **动量确认**：策略检查最近三根完成的 K 线上的 Momentum 数值，只要其中任何一个值与基准水平 100 的偏差达到预设阈值（多空分别设置）即可视为有效。
3. **MACD 过滤**：做多要求 MACD 主线高于信号线；做空要求 MACD 主线低于信号线。
4. **仓位管理**：在开仓之前会自动平掉相反方向的持仓，保证账户中最多只有一个方向的仓位。

## 风险管理
通过 `StartProtection` 统一设置保护性订单：
- 可选的止损和止盈距离（以最小价格变动为单位）。
- 可选的追踪止损以及追踪步长。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 下单量（手数或合约数）。 | 1 |
| `FastMaLength` | 快速线性加权均线周期。 | 6 |
| `SlowMaLength` | 慢速线性加权均线周期。 | 85 |
| `MomentumPeriod` | Momentum 指标周期。 | 14 |
| `MomentumBuyThreshold` | 允许开多仓所需的 Momentum 偏差（相对于 100）。 | 0.3 |
| `MomentumSellThreshold` | 允许开空仓所需的 Momentum 偏差（相对于 100）。 | 0.3 |
| `MacdFast` | MACD 中快速 EMA 的周期。 | 12 |
| `MacdSlow` | MACD 中慢速 EMA 的周期。 | 26 |
| `MacdSignal` | MACD 信号线的 EMA 周期。 | 9 |
| `TakeProfitPips` | 止盈距离（价格步长数）。 | 50 |
| `StopLossPips` | 止损距离（价格步长数）。 | 20 |
| `TrailingStopPips` | 追踪止损距离（价格步长数）。 | 40 |
| `TrailingStepPips` | 追踪止损的调整步长。 | 10 |
| `CandleType` | 计算使用的 K 线类型/时间框架。 | 15 分钟 |
| `EnableLongs` | 是否允许做多。 | true |
| `EnableShorts` | 是否允许做空。 | true |

## 指标
- **LinearWeightedMovingAverage**：用于趋势方向判定的快慢线。
- **Momentum**：确认价格动能是否足够强劲。
- **MovingAverageConvergenceDivergenceSignal**：通过 MACD 主线与信号线的关系进行方向过滤。

## 使用提示
- Momentum 阈值以最近三根已完成 K 线的数值为准，以模拟原始 MQL 策略的行为。
- 将止损、止盈或追踪止损参数设置为零即可关闭相应的保护机制。
- 策略需要交易品种提供价格最小变动和小数位信息，才能正确计算点值。
