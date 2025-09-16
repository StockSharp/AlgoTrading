# XFatlXSatlCloud Duplex

## 概述
XFatlXSatlCloud Duplex 是从原始 MQL5 专家顾问移植而来的双向策略。策略使用 XFatlXSatlCloud 指标，该指标将快速的 FATL 数字滤波器与较慢的 SATL 滤波器结合，并使用可配置的移动平均线对两条曲线进行平滑。多头与空头可以分别设置不同的时间框架、平滑方法以及价格来源。

## 交易逻辑
策略仅在蜡烛收盘后进行计算。多头与空头逻辑由两个独立的订阅驱动，每个订阅都会调用 C# 实现的 XFatlXSatlCloud 指标，并遵循以下规则：

- **多头入场**：当 `LongSignalBar` 指定的柱子上快线向上穿越慢线时触发。如果当前持有空头，并且 `ShortAllowClose` 允许平仓，则会先平掉空头，再按 `LongVolume` 下市价买单，并记录入场价用于风控。
- **多头离场**：当快线在相同偏移柱子下穿慢线时触发。若设置了 `LongStopLoss` 或 `LongTakeProfit`，当当前蜡烛的最高价/最低价触发这些绝对距离时，也会提前平仓。
- **空头入场**：当 `ShortSignalBar` 指定的柱子上快线向下穿越慢线时触发。若存在多头并且 `LongAllowClose` 允许平仓，则先平掉多头，然后按 `ShortVolume` 提交市价卖单。
- **空头离场**：当快线在相同偏移柱子上穿慢线时触发。`ShortStopLoss` 和 `ShortTakeProfit` 会监控当前蜡烛的极值，满足条件时立即离场。

所有信号均基于收盘完成的数据，从而重现原版 MQL EA 的运行方式。

## 风险控制
策略分别跟踪多头和空头的入场价。只要启用了相应的 `AllowClose` 参数，当当前蜡烛的高/低价突破设定的止损或止盈绝对距离时，将立即平仓。所有距离都以标的资产的绝对价格单位表示。

## 参数说明
| 组别 | 参数 | 说明 |
| --- | --- | --- |
| Trading | `LongVolume` | 多头建仓数量（必须为正数）。 |
| Trading | `ShortVolume` | 空头建仓数量（必须为正数）。 |
| Trading | `LongAllowOpen` | 是否允许开多。 |
| Trading | `LongAllowClose` | 是否允许多头平仓（止损/止盈与金叉离场均依赖该设置）。 |
| Trading | `ShortAllowOpen` | 是否允许开空。 |
| Trading | `ShortAllowClose` | 是否允许空头平仓。 |
| Signals | `LongSignalBar` | 检查多头信号时回看完成柱的数量。 |
| Signals | `ShortSignalBar` | 检查空头信号时回看完成柱的数量。 |
| Data | `LongCandleType` | 多头指标订阅使用的蜡烛类型（时间框架）。 |
| Data | `ShortCandleType` | 空头指标订阅使用的蜡烛类型。 |
| Indicators | `LongMethod1` | 多头 FATL 输出的平滑方法（支持 SMA、EMA、SMMA、LWMA、Jurik、ZeroLag、Kaufman）。 |
| Indicators | `LongLength1` | 多头快线平滑长度。 |
| Indicators | `LongPhase1` | 多头快线平滑的相位参数（为兼容保留，主要用于 Jurik）。 |
| Indicators | `LongMethod2` | 多头 SATL 输出的平滑方法。 |
| Indicators | `LongLength2` | 多头慢线平滑长度。 |
| Indicators | `LongPhase2` | 多头慢线平滑的相位参数。 |
| Indicators | `LongAppliedPrice` | 多头指标使用的价格类型（收盘、开盘、中值、典型价、加权价、平均价、四价、趋势跟随、Demark 等）。 |
| Indicators | `ShortMethod1` | 空头快线平滑方法。 |
| Indicators | `ShortLength1` | 空头快线平滑长度。 |
| Indicators | `ShortPhase1` | 空头快线相位参数。 |
| Indicators | `ShortMethod2` | 空头慢线平滑方法。 |
| Indicators | `ShortLength2` | 空头慢线平滑长度。 |
| Indicators | `ShortPhase2` | 空头慢线相位参数。 |
| Indicators | `ShortAppliedPrice` | 空头指标使用的价格类型。 |
| Risk | `LongStopLoss` | 多头止损的绝对价格距离（0 表示禁用）。 |
| Risk | `LongTakeProfit` | 多头止盈的绝对价格距离（0 表示禁用）。 |
| Risk | `ShortStopLoss` | 空头止损的绝对价格距离（0 表示禁用）。 |
| Risk | `ShortTakeProfit` | 空头止盈的绝对价格距离（0 表示禁用）。 |

## 实现细节
- XFatlXSatlCloud 指标在 C# 中以高层 API 重新实现，先使用原始 FATL/SATL FIR 系数计算，再交由所选平滑指标处理。
- 目前提供的平滑方法包括 `Sma`、`Ema`、`Smma`、`Lwma`、`Jurik`、`ZeroLag`、`Kaufman`。MQL 中的其他选项（如 Parabolic、T3）暂不支持。
- `LongSignalBar` 与 `ShortSignalBar` 与原版参数含义一致，取值 1 表示“使用上一根完成蜡烛”来判断交叉。
- 止损/止盈距离以绝对价格计量，基于当前蜡烛的高低价与记录的入场价比较，不依赖经纪商的点值设置。
