# Mean Reversion Momentum 策略

## 概述
Mean Reversion 策略是 MetaTrader 专家顾问 *Mean reversion.mq4* 的移植版。StockSharp 版本保留了原始思想：在连续收盘价下跌后买入，在连续上涨后卖出。信号需要三个过滤条件：两条线性加权移动平均线的趋势一致、更高周期的 Momentum 强度，以及月线 MACD。

进入仓位后，策略实现了原始 EA 的资金管理：以点数定义的止损/止盈、可选的保本位移动作，以及随着盈利增加而锁定利润的追踪止损。

## 交易逻辑
1. **信号周期** – 使用所选蜡烛序列运行（默认 15 分钟）。
2. **超卖/超买检测** – 收集最近 `BarsToCount` 个收盘价。做多时要求最新收盘价低于所有之前的收盘价；做空则相反。
3. **趋势过滤** – 快速 LWMA (`FastMaLength`) 必须高于慢速 LWMA (`SlowMaLength`) 才能做多，做空则需要低于。
4. **动量过滤** – Momentum 指标（周期 `MomentumLength`）在 MetaTrader 风格的更高周期上计算（M15→H1、H1→D1 等）。最近三次 Momentum 中至少一次偏离 100 的幅度大于 `MomentumThreshold`。
5. **MACD 确认** – 月线 MACD (12/26/9) 的主线必须高于信号线才能做多，低于信号线才能做空。

满足所有条件后，策略按 `OrderVolume` 下市价单。出现反向信号时会先平掉当前仓位再反向开仓。

## 仓位管理
- **止损/止盈** – 使用 `StopLossPips` 和 `TakeProfitPips` 以点数设置。
- **保本位** – 启用后，当浮盈超过 `BreakEvenTriggerPips` 时，将止损移动到入场价并额外添加 `BreakEvenOffsetPips`。
- **追踪止损** – 若 `EnableTrailing` 为真且盈利超过 `TrailingStopPips`，止损将以 `TrailingStepPips` 的距离跟随价格。

所有点数计算都会结合品种的最小报价步长，行为与 MetaTrader 保持一致。

## 参数
| 名称 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 市价单成交量。 | `1` |
| `CandleType` | 生成信号的主蜡烛序列。 | `M15` |
| `BarsToCount` | 检测超买/超卖所需的收盘价数量。 | `10` |
| `FastMaLength` | 快速 LWMA 周期。 | `6` |
| `SlowMaLength` | 慢速 LWMA 周期。 | `85` |
| `MomentumLength` | 高周期 Momentum 的周期。 | `14` |
| `MomentumThreshold` | Momentum 偏离 100 的最小幅度。 | `0.3` |
| `StopLossPips` | 止损距离（点）。 | `20` |
| `TakeProfitPips` | 止盈距离（点）。 | `50` |
| `UseBreakEven` | 是否启用保本位。 | `false` |
| `BreakEvenTriggerPips` | 触发保本位所需的盈利（点）。 | `30` |
| `BreakEvenOffsetPips` | 移动止损时额外加入的点数。 | `30` |
| `EnableTrailing` | 是否启用追踪止损。 | `true` |
| `TrailingStopPips` | 开始追踪所需的盈利（点）。 | `40` |
| `TrailingStepPips` | 追踪止损与价格保持的距离。 | `40` |

## 备注
- Momentum 的高周期遵循 MetaTrader 的阶梯：M1→M15、M5→M30、M15→H1、M30→H4、H1→D1、H4→W1、D1→MN1、W1→MN1。
- MACD 始终使用月线（MN1）数据计算。
- 策略仅支持基于时间的蜡烛类型，不支持点数或跳动图表。
