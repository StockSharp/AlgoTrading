# Exp XPeriod Candle X2 策略

## 概述
Exp XPeriod Candle X2 在 StockSharp 高级 API 上复刻了原始的 MetaTrader 专家顾问。策略在两个时间框架上构建平滑的合成蜡烛，并将指定周期内的延迟开盘价与最新的平滑收盘价进行比较。较高时间框架的蜡烛颜色定义趋势方向，工作时间框架等待颜色翻转触发进出场信号。可选的止损和止盈参数与原始脚本的资金管理设定保持一致。

## 工作机制
- **趋势判定**：较高时间框架对开盘价和收盘价应用所选平滑方法。每根完成的蜡烛都会把平滑收盘价与 `TrendPeriod` 根之前的平滑开盘价进行比较。收盘价高于延迟开盘价得到多头颜色（0），低于则得到空头颜色（2）。`TrendSignalBar` 指定的颜色决定全局趋势是多头（+1）、空头（-1）还是中性。
- **入场逻辑**：工作时间框架应用相同的平滑过程，并保存 `EntrySignalBar` 及其上一根的颜色。当趋势为下行且当前颜色为 0、上一根颜色为 2 时产生做空信号；趋势为上行且当前颜色为 2、上一根颜色为 0 时产生做多信号，与原版 XPeriodCandle 的翻转规则一致。
- **仓位管理**：`CloseLongOnTrendFlip`、`CloseShortOnTrendFlip`、`CloseLongOnEntrySignal`、`CloseShortOnEntrySignal` 等开关控制在趋势反转或入场级别反向时平仓。新下单的手数为 `Volume + |Position|`，与原始 EA 一样在反向信号时先平仓再反向开仓。
- **风险控制**：止损和止盈距离以价格最小变动单位表示（`StopLossTicks`、`TakeProfitTicks`），仅在对应布尔值启用时生效。
- **平滑方式**：使用 StockSharp 内置移动平均替代 SmoothAlgorithms 库，可选简单、指数、平滑（SMMA）、加权、Hull、Kaufman 自适应和 Jurik。`TrendPhase` 与 `EntryPhase` 仅在使用 Jurik 时生效，并限制在 ±100 之间。

## 参数
| 参数 | 说明 |
| --- | --- |
| `TrendCandleType` | 用于趋势过滤的高时间框架蜡烛类型。 |
| `EntryCandleType` | 用于进出场信号的工作时间框架蜡烛类型。 |
| `TrendPeriod` | 计算趋势延迟开盘价时使用的平滑蜡烛数量。 |
| `EntryPeriod` | 计算入场延迟开盘价时使用的平滑蜡烛数量。 |
| `TrendLength` | 趋势时间框架的平滑长度。 |
| `EntryLength` | 入场时间框架的平滑长度。 |
| `TrendPhase` | 趋势时间框架的 Jurik 相位参数（其他平滑方式忽略）。 |
| `EntryPhase` | 入场时间框架的 Jurik 相位参数（其他平滑方式忽略）。 |
| `TrendSignalBar` | 用于读取趋势颜色的偏移量（`1` 表示最新收盘蜡烛）。 |
| `EntrySignalBar` | 用于读取入场颜色的偏移量（`1` 为最新收盘蜡烛，`2` 为其前一根）。 |
| `TrendSmoothing` | 高时间框架使用的平滑类型。 |
| `EntrySmoothing` | 工作时间框架使用的平滑类型。 |
| `EnableLongEntries` | 允许在满足多头条件时建多仓。 |
| `EnableShortEntries` | 允许在满足空头条件时建空仓。 |
| `CloseLongOnTrendFlip` | 当高时间框架转为空头时平掉多仓。 |
| `CloseShortOnTrendFlip` | 当高时间框架转为多头时平掉空仓。 |
| `CloseLongOnEntrySignal` | 当入场时间框架出现空头颜色时平掉多仓。 |
| `CloseShortOnEntrySignal` | 当入场时间框架出现多头颜色时平掉空仓。 |
| `UseStopLoss` | 启用以跳动单位表示的止损。 |
| `StopLossTicks` | 止损距离（以价格跳动为单位）。 |
| `UseTakeProfit` | 启用以跳动单位表示的止盈。 |
| `TakeProfitTicks` | 止盈距离（以价格跳动为单位）。 |

## 说明
- 延迟开盘价逻辑会保留周期内最早的平滑开盘价，与原指标的循环数组一致。
- 当 `TrendCandleType` 与 `EntryCandleType` 相同时只会创建一次订阅，但双颜色逻辑仍正常运行。
- 请合理设置 `Volume`，反手信号会自动加上当前绝对仓位，以符合原 EA 的手数处理方式。
