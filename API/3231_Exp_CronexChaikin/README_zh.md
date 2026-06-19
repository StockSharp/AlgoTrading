# Exp Cronex Chaikin 策略

该策略将 MetaTrader 专家顾问 **Exp_CronexChaikin.mq5** 移植到 StockSharp 高级 API。原始机器人基于累积/派发数据重建 Chaikin 振荡指标，再通过 Cronex “XMA” 滤波器进行两次平滑，并在快慢线交叉时进行交易。StockSharp 版本复刻了相同的流程，并将每个阶段暴露为可配置参数。

## 交易逻辑

1. 订阅所选的蜡烛序列（`CandleType`）。
2. 对每根已完成的蜡烛，根据 `VolumeSource`（跳动量或真实成交量）重新计算累积/派发 (AD) 曲线。
3. 使用 `ChaikinMethod`、`ChaikinFastPeriod` 和 `ChaikinSlowPeriod` 对 AD 曲线做两次移动平均，并取其差值得到 Chaikin 振荡值。
4. 采用由 `SmoothingMethod`、`FastPeriod`、`SlowPeriod` 与 `Phase` 控制的 Cronex 平滑器，对振荡值再次进行两级平滑，得到原指标中的快线与信号线。
5. 回看 `SignalBar` 根已完成的蜡烛，对比该根蜡烛及其前一根蜡烛上的两条 Cronex 线。
6. 当快线在信号线上方时，策略可以先平掉空头（若 `SellCloseEnabled` 为真），并在 `BuyOpenEnabled` 允许的情况下、且回溯蜡烛上出现向上交叉时开多。
7. 当快线在信号线下方时，执行与上一步相反的空头操作，由 `SellOpenEnabled` 与 `BuyCloseEnabled` 控制。
8. 每次开仓后，使用 `StopLoss` 与 `TakeProfit`（以点为单位）重新设置止损与止盈距离。

策略始终保持单一净仓位。当信号方向改变时，会自动将平仓量与新开仓量合并，以模拟 MetaTrader 净值账户的行为。

## 指标与平滑设置

- **Chaikin 振荡器**：对 AD 曲线应用所选的 `ChaikinMethod` 移动平均类型，可选简单、指数、平滑以及线性加权等方式。
- **Cronex 平滑器**：`SmoothingMethod` 参数覆盖 Cronex XMA 系列（SMA、EMA、SMMA、LWMA、Jurik JJMA/JurX、Parabolic MA、T3、VIDYA、AMA）。`Phase` 参数用于调节 Jurik 类滤波器，与 MQL 实现一致。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 构建指标所用的蜡烛类型，默认是 4 小时时间框架。 |
| `ChaikinMethod` | Chaikin 振荡器内部使用的移动平均方法。 |
| `ChaikinFastPeriod` / `ChaikinSlowPeriod` | 应用于 AD 曲线的快、慢周期。 |
| `SmoothingMethod` | 作用于 Chaikin 振荡值的 Cronex 平滑算法。 |
| `FastPeriod` / `SlowPeriod` | Cronex 快线与信号线的周期长度。 |
| `Phase` | Jurik 类平滑器的相位参数，范围 -100~+100。 |
| `VolumeSource` | 计算 AD 曲线时选用跳动量或真实成交量。 |
| `SignalBar` | 回溯多少根已完成蜡烛以判定交叉信号。 |
| `BuyOpenEnabled` / `SellOpenEnabled` | 是否允许开多或开空。 |
| `BuyCloseEnabled` / `SellCloseEnabled` | 是否允许在反向信号出现时平掉已有仓位。 |
| `TakeProfit` / `StopLoss` | 每次开仓后设置的止盈/止损距离（以点计）。 |
| `Volume` | StockSharp 中的标准下单手数，对应原专家顾问的手数设置。 |

## 与 MQL 版本的差异

- `TradeAlgorithms.mqh` 中的资金管理与滑点模块由 `Volume`、`SetStopLoss`、`SetTakeProfit` 等内置方法替代。
- StockSharp 版本只在蜡烛收盘后重新计算 AD 曲线，便于回测与实时保持一致。
- Cronex 平滑器直接使用 StockSharp 指标实现：Jurik 滤波器基于 `JurikMovingAverage`（支持相位参数），VIDYA 与 ParMa 使用与其他 Cronex 策略相同的指数近似。
